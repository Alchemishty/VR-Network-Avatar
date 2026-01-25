using System.Collections.Generic;
using UnityEngine;

namespace VRNetworkAvatar
{
    /// <summary>
    /// XR rig provider implementation for Meta/Oculus SDK.
    /// When Meta XR SDK is installed, this uses OVRCameraRig for tracking.
    /// Otherwise, it falls back to finding transforms by name.
    /// </summary>
    public class OVRRigProvider : MonoBehaviour, IXRRigProvider
    {
        [Header("Manual References (Optional)")]
        [Tooltip("If not set, will search for OVRCameraRig in scene")]
        [SerializeField] private Transform headTransform;
        [SerializeField] private Transform leftHandTransform;
        [SerializeField] private Transform rightHandTransform;
        [SerializeField] private Transform rigRoot;

        [Header("Finger Tracking")]
        [SerializeField] private string leftFingerTag = "LeftFinger";
        [SerializeField] private string rightFingerTag = "RightFinger";

        private bool isInitialized;
        private List<Transform> leftFingerTransforms = new List<Transform>();
        private List<Transform> rightFingerTransforms = new List<Transform>();

        public Transform HeadTransform => headTransform;
        public Transform LeftHandTransform => leftHandTransform;
        public Transform RightHandTransform => rightHandTransform;
        public Transform RigRootTransform => rigRoot;
        public bool SupportsFingerTracking => leftFingerTransforms.Count > 0 || rightFingerTransforms.Count > 0;
        public bool IsInitialized => isInitialized;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized) return;

            // Try to find OVRCameraRig by name
            if (rigRoot == null)
            {
                var ovrRigGO = GameObject.Find("OVRCameraRig");
                if (ovrRigGO != null)
                {
                    rigRoot = ovrRigGO.transform;
                }
            }

            // Try to find transforms using OVR hierarchy structure
            if (rigRoot != null)
            {
                // Try standard OVR hierarchy paths
                if (headTransform == null)
                {
                    headTransform = FindChildByPath(rigRoot, "TrackingSpace/CenterEyeAnchor");
                }
                if (leftHandTransform == null)
                {
                    // Try controller anchor first, then hand anchor
                    leftHandTransform = FindChildByPath(rigRoot, "TrackingSpace/LeftHandAnchor/LeftControllerAnchor");
                    if (leftHandTransform == null)
                    {
                        leftHandTransform = FindChildByPath(rigRoot, "TrackingSpace/LeftHandAnchor");
                    }
                }
                if (rightHandTransform == null)
                {
                    rightHandTransform = FindChildByPath(rigRoot, "TrackingSpace/RightHandAnchor/RightControllerAnchor");
                    if (rightHandTransform == null)
                    {
                        rightHandTransform = FindChildByPath(rigRoot, "TrackingSpace/RightHandAnchor");
                    }
                }
            }

            // Try to use OVRCameraRig component via reflection (if OVR SDK is installed)
            TryInitializeFromOVRCameraRig();

            // Cache finger transforms by tag
            CacheFingerTransforms();

            isInitialized = headTransform != null && leftHandTransform != null && rightHandTransform != null;

            if (!isInitialized)
            {
                Debug.LogWarning("[OVRRigProvider] Failed to initialize - some transforms are missing. " +
                    $"Head: {(headTransform != null ? "OK" : "MISSING")}, " +
                    $"LeftHand: {(leftHandTransform != null ? "OK" : "MISSING")}, " +
                    $"RightHand: {(rightHandTransform != null ? "OK" : "MISSING")}");
            }
        }

        private void TryInitializeFromOVRCameraRig()
        {
            if (rigRoot == null) return;

            // Try to get OVRCameraRig component using reflection to avoid compile-time dependency
            var ovrCameraRigType = System.Type.GetType("OVRCameraRig, Oculus.VR");
            if (ovrCameraRigType == null)
            {
                // Try alternative assembly name
                ovrCameraRigType = System.Type.GetType("OVRCameraRig, Meta.XR.Core");
            }

            if (ovrCameraRigType != null)
            {
                var ovrCameraRig = rigRoot.GetComponent(ovrCameraRigType);
                if (ovrCameraRig != null)
                {
                    // Get centerEyeAnchor property
                    if (headTransform == null)
                    {
                        var centerEyeProp = ovrCameraRigType.GetProperty("centerEyeAnchor");
                        if (centerEyeProp != null)
                        {
                            headTransform = centerEyeProp.GetValue(ovrCameraRig) as Transform;
                        }
                    }

                    // Get leftControllerAnchor property
                    if (leftHandTransform == null)
                    {
                        var leftControllerProp = ovrCameraRigType.GetProperty("leftControllerAnchor");
                        if (leftControllerProp != null)
                        {
                            leftHandTransform = leftControllerProp.GetValue(ovrCameraRig) as Transform;
                        }
                    }

                    // Get rightControllerAnchor property
                    if (rightHandTransform == null)
                    {
                        var rightControllerProp = ovrCameraRigType.GetProperty("rightControllerAnchor");
                        if (rightControllerProp != null)
                        {
                            rightHandTransform = rightControllerProp.GetValue(ovrCameraRig) as Transform;
                        }
                    }
                }
            }
        }

        private Transform FindChildByPath(Transform root, string path)
        {
            var parts = path.Split('/');
            var current = root;

            foreach (var part in parts)
            {
                current = current.Find(part);
                if (current == null) return null;
            }

            return current;
        }

        private void CacheFingerTransforms()
        {
            leftFingerTransforms.Clear();
            rightFingerTransforms.Clear();

            if (!string.IsNullOrEmpty(leftFingerTag))
            {
                try
                {
                    foreach (var obj in GameObject.FindGameObjectsWithTag(leftFingerTag))
                    {
                        leftFingerTransforms.Add(obj.transform);
                    }
                }
                catch (UnityException)
                {
                    // Tag doesn't exist
                }
            }

            if (!string.IsNullOrEmpty(rightFingerTag))
            {
                try
                {
                    foreach (var obj in GameObject.FindGameObjectsWithTag(rightFingerTag))
                    {
                        rightFingerTransforms.Add(obj.transform);
                    }
                }
                catch (UnityException)
                {
                    // Tag doesn't exist
                }
            }
        }

        public IReadOnlyList<Transform> GetLeftFingerTransforms()
        {
            return leftFingerTransforms;
        }

        public IReadOnlyList<Transform> GetRightFingerTransforms()
        {
            return rightFingerTransforms;
        }
    }
}
