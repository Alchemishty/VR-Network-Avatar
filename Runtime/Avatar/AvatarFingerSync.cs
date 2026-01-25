using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace VRNetworkAvatar
{
    /// <summary>
    /// Optional component for synchronizing finger tracking data.
    /// Add this to your avatar prefab if you want finger tracking support.
    ///
    /// This is a separate component from NetworkVRAvatar to keep finger tracking optional
    /// and reduce complexity for projects that don't need it.
    /// </summary>
    public class AvatarFingerSync : NetworkBehaviour
    {
        [Header("XR Rig Provider")]
        [SerializeField] private MonoBehaviour xrRigProviderComponent;
        private IXRRigProvider xrRigProvider;

        [Header("Avatar Finger Transforms")]
        [Tooltip("Avatar's left hand finger transforms (in order matching XR rig)")]
        [SerializeField] private List<Transform> avatarLeftFingers = new List<Transform>();

        [Tooltip("Avatar's right hand finger transforms (in order matching XR rig)")]
        [SerializeField] private List<Transform> avatarRightFingers = new List<Transform>();

        [Header("Sync Settings")]
        [SerializeField] private AvatarSyncSettings syncSettings;
        [SerializeField] private float rotationThreshold = 0.1f;

        // XR rig finger transforms (from provider)
        private IReadOnlyList<Transform> xrLeftFingers;
        private IReadOnlyList<Transform> xrRightFingers;

        // Previous frame rotations for threshold comparison
        private Quaternion[] lastLeftFingerRotations;
        private Quaternion[] lastRightFingerRotations;

        private float RotationThreshold => syncSettings != null ? syncSettings.fingerRotationThreshold : rotationThreshold;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner) return;

            InitializeXRRig();
            InitializeFingerArrays();
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
                // Look for OVRRigProvider first (primary - supports finger tracking)
                var ovrProvider = FindObjectOfType<OVRRigProvider>();
                if (ovrProvider != null)
                {
                    xrRigProvider = ovrProvider;
                }
                else
                {
                    // Fallback to GenericXRRigProvider (usually doesn't support fingers)
                    var genericProvider = FindObjectOfType<GenericXRRigProvider>();
                    if (genericProvider != null)
                    {
                        xrRigProvider = genericProvider;
                    }
                }
            }

            if (xrRigProvider == null)
            {
                Debug.LogWarning("[AvatarFingerSync] No IXRRigProvider found! Finger tracking disabled.");
                enabled = false;
                return;
            }

            if (!xrRigProvider.SupportsFingerTracking)
            {
                Debug.LogWarning("[AvatarFingerSync] XR provider does not support finger tracking. Disabling.");
                enabled = false;
                return;
            }

            // Ensure provider is initialized
            if (!xrRigProvider.IsInitialized)
            {
                xrRigProvider.Initialize();
            }

            // Get finger transforms from provider
            xrLeftFingers = xrRigProvider.GetLeftFingerTransforms();
            xrRightFingers = xrRigProvider.GetRightFingerTransforms();
        }

        private void InitializeFingerArrays()
        {
            if (avatarLeftFingers.Count > 0)
            {
                lastLeftFingerRotations = new Quaternion[avatarLeftFingers.Count];
            }

            if (avatarRightFingers.Count > 0)
            {
                lastRightFingerRotations = new Quaternion[avatarRightFingers.Count];
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner || xrRigProvider == null || !xrRigProvider.IsInitialized) return;

            UpdateFingers();
        }

        private void UpdateFingers()
        {
            UpdateFingerSet(avatarLeftFingers, xrLeftFingers, lastLeftFingerRotations, "left");
            UpdateFingerSet(avatarRightFingers, xrRightFingers, lastRightFingerRotations, "right");
        }

        private void UpdateFingerSet(List<Transform> avatarFingers, IReadOnlyList<Transform> xrFingers,
            Quaternion[] lastRotations, string handName)
        {
            if (avatarFingers == null || xrFingers == null || lastRotations == null) return;

            // Validate counts match
            if (avatarFingers.Count != xrFingers.Count)
            {
                Debug.LogWarning($"[AvatarFingerSync] Mismatch in {handName} finger counts: " +
                    $"Avatar has {avatarFingers.Count}, XR rig has {xrFingers.Count}");
                return;
            }

            for (int i = 0; i < avatarFingers.Count; i++)
            {
                if (avatarFingers[i] == null || xrFingers[i] == null) continue;

                Quaternion newRotation = xrFingers[i].rotation;

                // Apply with threshold check
                if (Quaternion.Angle(newRotation, lastRotations[i]) > RotationThreshold)
                {
                    avatarFingers[i].rotation = newRotation;
                    lastRotations[i] = newRotation;
                }
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

            if (!xrRigProvider.SupportsFingerTracking)
            {
                Debug.LogWarning("[AvatarFingerSync] New XR provider does not support finger tracking. Disabling.");
                enabled = false;
                return;
            }

            xrLeftFingers = xrRigProvider.GetLeftFingerTransforms();
            xrRightFingers = xrRigProvider.GetRightFingerTransforms();
        }

        /// <summary>
        /// Apply sync settings from a ScriptableObject.
        /// </summary>
        public void ApplySyncSettings(AvatarSyncSettings settings)
        {
            syncSettings = settings;
        }

        /// <summary>
        /// Set avatar finger transforms at runtime.
        /// </summary>
        public void SetAvatarFingers(List<Transform> leftFingers, List<Transform> rightFingers)
        {
            avatarLeftFingers = leftFingers ?? new List<Transform>();
            avatarRightFingers = rightFingers ?? new List<Transform>();
            InitializeFingerArrays();
        }
    }
}
