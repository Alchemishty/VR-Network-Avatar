using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace VRNetworkAvatar
{
    /// <summary>
    /// Handles network synchronization of VR player avatar movements.
    /// Syncs head, hands, and body transforms based on XR rig tracking.
    ///
    /// This component does NOT include finger tracking - use AvatarFingerSync for that.
    /// </summary>
    public class NetworkVRAvatar : NetworkBehaviour
    {
        [Header("XR Rig Provider")]
        [SerializeField] private MonoBehaviour xrRigProviderComponent;
        private IXRRigProvider xrRigProvider;

        [Header("Avatar Body Parts")]
        [SerializeField] private Transform avatarHead;
        [SerializeField] private Transform avatarLeftHand;
        [SerializeField] private Transform avatarRightHand;
        [SerializeField] private Transform avatarBody;

        [Header("Position Offsets")]
        [SerializeField] private Vector3 leftHandPositionOffset;
        [SerializeField] private Vector3 rightHandPositionOffset;
        [SerializeField] private Vector3 headPositionOffset;
        [SerializeField] private Vector3 bodyPositionOffset;

        [Header("Rotation Offsets")]
        [SerializeField] private Quaternion leftHandRotationOffset = Quaternion.identity;
        [SerializeField] private Quaternion rightHandRotationOffset = Quaternion.identity;

        [Header("Sync Settings")]
        [SerializeField] private AvatarSyncSettings syncSettings;

        [Header("Constraints")]
        [SerializeField] private float maxArmLength = 0.7f;
        [SerializeField] private float headRotationClampAngle = 45f;

        [Header("Local Avatar Settings")]
        [SerializeField] private string localAvatarLayer = "LocalAvatar";
        [SerializeField] private bool disableLocalColliders = true;

        // Cached transforms from XR rig
        private Transform xrHead;
        private Transform xrLeftHand;
        private Transform xrRightHand;
        private Transform xrRigRoot;

        // Previous frame state for threshold comparison
        private Vector3 lastHeadPosition, lastLeftPosition, lastRightPosition, lastBodyPosition;
        private Quaternion lastHeadRotation, lastLeftRotation, lastRightRotation, lastBodyRotation;

        // Default thresholds (used if no syncSettings provided)
        private const float DefaultPositionThreshold = 0.001f;
        private const float DefaultRotationThreshold = 0.1f;

        private float PositionThreshold => syncSettings != null ? syncSettings.positionThreshold : DefaultPositionThreshold;
        private float RotationThreshold => syncSettings != null ? syncSettings.rotationThreshold : DefaultRotationThreshold;

        public Transform AvatarHead => avatarHead;
        public Transform AvatarLeftHand => avatarLeftHand;
        public Transform AvatarRightHand => avatarRightHand;
        public Transform AvatarBody => avatarBody;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Register with manager
            StartCoroutine(RegisterWithManager());

            if (!IsOwner) return;

            // Setup local avatar
            SetupLocalAvatar();
            InitializeXRRig();
        }

        private IEnumerator RegisterWithManager()
        {
            // Wait for manager to be available
            while (VRNetworkAvatarManager.Instance == null)
            {
                yield return null;
            }

            VRNetworkAvatarManager.Instance.RegisterAvatar(gameObject);
        }

        private void SetupLocalAvatar()
        {
            // Set layer for local avatar (to hide from local camera if needed)
            if (!string.IsNullOrEmpty(localAvatarLayer))
            {
                int layer = LayerMask.NameToLayer(localAvatarLayer);
                if (layer >= 0)
                {
                    SetLayerRecursively(gameObject, layer);
                }
            }

            // Disable colliders on local avatar to prevent self-collision
            if (disableLocalColliders)
            {
                DisableAllColliders(transform);
            }
        }

        private void InitializeXRRig()
        {
            // Try to get IXRRigProvider from assigned component
            if (xrRigProviderComponent != null)
            {
                xrRigProvider = xrRigProviderComponent as IXRRigProvider;
            }

            // Try to find provider in scene if not assigned
            if (xrRigProvider == null)
            {
                // Look for OVRRigProvider first (primary)
                var ovrProvider = FindObjectOfType<OVRRigProvider>();
                if (ovrProvider != null)
                {
                    xrRigProvider = ovrProvider;
                }
                else
                {
                    // Fallback to GenericXRRigProvider
                    var genericProvider = FindObjectOfType<GenericXRRigProvider>();
                    if (genericProvider != null)
                    {
                        xrRigProvider = genericProvider;
                    }
                }
            }

            if (xrRigProvider == null)
            {
                Debug.LogError("[NetworkVRAvatar] No IXRRigProvider found! Avatar tracking will not work.");
                return;
            }

            // Ensure provider is initialized
            if (!xrRigProvider.IsInitialized)
            {
                xrRigProvider.Initialize();
            }

            // Cache transforms
            xrHead = xrRigProvider.HeadTransform;
            xrLeftHand = xrRigProvider.LeftHandTransform;
            xrRightHand = xrRigProvider.RightHandTransform;
            xrRigRoot = xrRigProvider.RigRootTransform;

            if (xrHead == null || xrLeftHand == null || xrRightHand == null)
            {
                Debug.LogError("[NetworkVRAvatar] XR rig provider missing required transforms!");
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner || xrRigProvider == null || !xrRigProvider.IsInitialized) return;

            UpdateHandTransform(ref lastLeftPosition, ref lastLeftRotation, avatarLeftHand, xrLeftHand, leftHandPositionOffset, leftHandRotationOffset, isHand: true);
            UpdateHandTransform(ref lastRightPosition, ref lastRightRotation, avatarRightHand, xrRightHand, rightHandPositionOffset, rightHandRotationOffset, isHand: true);
            UpdateHeadTransform();
            UpdateBodyTransform();
        }

        private void UpdateHeadTransform()
        {
            if (avatarHead == null || xrHead == null) return;

            Vector3 newPosition = xrHead.position;
            if (xrRigRoot != null)
            {
                newPosition += xrRigRoot.TransformVector(headPositionOffset);
            }
            else
            {
                newPosition += headPositionOffset;
            }

            Quaternion newRotation = xrHead.rotation;

            // Clamp head rotation to prevent unnatural angles
            Vector3 eulerRotation = newRotation.eulerAngles;

            // Normalize to -180 to 180 range
            if (eulerRotation.x > 180f) eulerRotation.x -= 360f;
            if (eulerRotation.z > 180f) eulerRotation.z -= 360f;

            // Clamp X and Z rotation
            eulerRotation.x = Mathf.Clamp(eulerRotation.x, -headRotationClampAngle, headRotationClampAngle);
            eulerRotation.z = Mathf.Clamp(eulerRotation.z, -headRotationClampAngle, headRotationClampAngle);

            newRotation = Quaternion.Euler(eulerRotation.x, eulerRotation.y, eulerRotation.z);

            // Apply with threshold check
            if (Vector3.SqrMagnitude(newPosition - lastHeadPosition) > PositionThreshold ||
                Quaternion.Angle(newRotation, lastHeadRotation) > RotationThreshold)
            {
                avatarHead.position = newPosition;
                avatarHead.rotation = newRotation;
                lastHeadPosition = newPosition;
                lastHeadRotation = newRotation;
            }
        }

        private void UpdateHandTransform(ref Vector3 lastPosition, ref Quaternion lastRotation,
            Transform avatarPart, Transform xrPart, Vector3 positionOffset, Quaternion rotationOffset, bool isHand)
        {
            if (avatarPart == null || xrPart == null) return;

            Vector3 newPosition = xrPart.position;
            if (xrRigRoot != null)
            {
                newPosition += xrRigRoot.TransformVector(positionOffset);
            }
            else
            {
                newPosition += positionOffset;
            }

            Quaternion newRotation = xrPart.rotation * rotationOffset;

            // Clamp hand distance from body (arm length constraint)
            if (isHand && avatarHead != null)
            {
                Vector3 bodyPosition = avatarHead.position;
                Vector3 direction = (newPosition - bodyPosition).normalized;
                float distance = Vector3.Distance(bodyPosition, newPosition);

                if (distance > maxArmLength)
                {
                    newPosition = bodyPosition + direction * maxArmLength;
                }
            }

            // Apply with threshold check
            if (Vector3.SqrMagnitude(newPosition - lastPosition) > PositionThreshold ||
                Quaternion.Angle(newRotation, lastRotation) > RotationThreshold)
            {
                avatarPart.position = newPosition;
                avatarPart.rotation = newRotation;
                lastPosition = newPosition;
                lastRotation = newRotation;
            }
        }

        private void UpdateBodyTransform()
        {
            if (avatarBody == null || avatarHead == null) return;

            Vector3 newBodyPosition = avatarHead.position + bodyPositionOffset;
            Quaternion newBodyRotation = Quaternion.Euler(
                avatarHead.rotation.eulerAngles.x,
                avatarHead.rotation.eulerAngles.y,
                avatarHead.rotation.eulerAngles.z
            );

            if (Vector3.SqrMagnitude(newBodyPosition - lastBodyPosition) > PositionThreshold ||
                Quaternion.Angle(newBodyRotation, lastBodyRotation) > RotationThreshold)
            {
                avatarBody.position = newBodyPosition;
                avatarBody.rotation = newBodyRotation;
                lastBodyPosition = newBodyPosition;
                lastBodyRotation = newBodyRotation;
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void DisableAllColliders(Transform parent)
        {
            foreach (Collider collider in parent.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
        }

        /// <summary>
        /// Manually set the XR rig provider at runtime.
        /// </summary>
        public void SetXRRigProvider(IXRRigProvider provider)
        {
            xrRigProvider = provider;
            if (!xrRigProvider.IsInitialized)
            {
                xrRigProvider.Initialize();
            }

            xrHead = xrRigProvider.HeadTransform;
            xrLeftHand = xrRigProvider.LeftHandTransform;
            xrRightHand = xrRigProvider.RightHandTransform;
            xrRigRoot = xrRigProvider.RigRootTransform;
        }

        /// <summary>
        /// Apply sync settings from a ScriptableObject.
        /// </summary>
        public void ApplySyncSettings(AvatarSyncSettings settings)
        {
            syncSettings = settings;
            if (settings != null)
            {
                maxArmLength = settings.maxArmLength;
                headRotationClampAngle = settings.headRotationClampAngle;
            }
        }
    }
}
