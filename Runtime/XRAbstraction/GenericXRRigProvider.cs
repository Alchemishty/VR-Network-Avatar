using System.Collections.Generic;
using UnityEngine;
#if VR_NETWORK_AVATAR_XRI
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
#endif

namespace VRNetworkAvatar
{
    /// <summary>
    /// Generic XR rig provider implementation using Unity XR Interaction Toolkit.
    /// Works with any XR device supported by Unity's XR system.
    /// </summary>
    public class GenericXRRigProvider : MonoBehaviour, IXRRigProvider
    {
        [Header("Transform References")]
        [Tooltip("The camera/head transform. If not set, will try to find Main Camera.")]
        [SerializeField] private Transform headTransform;

        [Tooltip("The left hand/controller transform.")]
        [SerializeField] private Transform leftHandTransform;

        [Tooltip("The right hand/controller transform.")]
        [SerializeField] private Transform rightHandTransform;

        [Tooltip("The root transform of the XR rig.")]
        [SerializeField] private Transform rigRoot;

#if VR_NETWORK_AVATAR_XRI
        [Header("XR Interaction Toolkit References (Optional)")]
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private XRBaseController leftController;
        [SerializeField] private XRBaseController rightController;
#endif

        private bool isInitialized;
        private static readonly List<Transform> emptyFingerList = new List<Transform>();

        public Transform HeadTransform => headTransform;
        public Transform LeftHandTransform => leftHandTransform;
        public Transform RightHandTransform => rightHandTransform;
        public Transform RigRootTransform => rigRoot;
        public bool SupportsFingerTracking => false; // Generic provider doesn't support finger tracking
        public bool IsInitialized => isInitialized;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized) return;

#if VR_NETWORK_AVATAR_XRI
            // Try to find XROrigin if not assigned
            if (xrOrigin == null)
            {
                xrOrigin = FindObjectOfType<XROrigin>();
            }

            if (xrOrigin != null)
            {
                if (rigRoot == null)
                {
                    rigRoot = xrOrigin.transform;
                }

                if (headTransform == null && xrOrigin.Camera != null)
                {
                    headTransform = xrOrigin.Camera.transform;
                }
            }

            // Try to find controllers if not assigned
            if (leftController == null || rightController == null)
            {
                var controllers = FindObjectsOfType<XRBaseController>();
                foreach (var controller in controllers)
                {
                    var interactor = controller.GetComponent<XRBaseControllerInteractor>();
                    if (interactor != null)
                    {
                        // Check controller handedness via XRNode
                        var controllerInteractor = controller.GetComponent<ActionBasedController>();
                        if (controllerInteractor != null)
                        {
                            // ActionBasedController doesn't have a direct handedness property
                            // We need to check the name or use ActionBasedController's model prefab
                            string name = controller.name.ToLower();
                            if (name.Contains("left") && leftController == null)
                            {
                                leftController = controller;
                            }
                            else if (name.Contains("right") && rightController == null)
                            {
                                rightController = controller;
                            }
                        }
                    }
                }
            }

            if (leftController != null && leftHandTransform == null)
            {
                leftHandTransform = leftController.transform;
            }

            if (rightController != null && rightHandTransform == null)
            {
                rightHandTransform = rightController.transform;
            }
#endif

            // Fallback: try to find by common names/tags
            if (rigRoot == null)
            {
                var xrRig = GameObject.Find("XR Origin") ??
                            GameObject.Find("XROrigin") ??
                            GameObject.Find("XR Rig") ??
                            GameObject.Find("XRRig");
                if (xrRig != null)
                {
                    rigRoot = xrRig.transform;
                }
            }

            if (headTransform == null)
            {
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    headTransform = mainCamera.transform;
                }
            }

            if (leftHandTransform == null)
            {
                var leftHand = GameObject.Find("LeftHand Controller") ??
                               GameObject.Find("Left Controller") ??
                               GameObject.Find("LeftHandAnchor");
                if (leftHand != null)
                {
                    leftHandTransform = leftHand.transform;
                }
            }

            if (rightHandTransform == null)
            {
                var rightHand = GameObject.Find("RightHand Controller") ??
                                GameObject.Find("Right Controller") ??
                                GameObject.Find("RightHandAnchor");
                if (rightHand != null)
                {
                    rightHandTransform = rightHand.transform;
                }
            }

            isInitialized = headTransform != null && leftHandTransform != null && rightHandTransform != null;

            if (!isInitialized)
            {
                Debug.LogWarning("[GenericXRRigProvider] Failed to initialize - some transforms are missing. " +
                    $"Head: {(headTransform != null ? "OK" : "MISSING")}, " +
                    $"LeftHand: {(leftHandTransform != null ? "OK" : "MISSING")}, " +
                    $"RightHand: {(rightHandTransform != null ? "OK" : "MISSING")}");
            }
        }

        public IReadOnlyList<Transform> GetLeftFingerTransforms()
        {
            return emptyFingerList;
        }

        public IReadOnlyList<Transform> GetRightFingerTransforms()
        {
            return emptyFingerList;
        }
    }
}
