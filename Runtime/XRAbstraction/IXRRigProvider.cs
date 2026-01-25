using System.Collections.Generic;
using UnityEngine;

namespace VRNetworkAvatar
{
    /// <summary>
    /// Interface for XR platform abstraction.
    /// Implement this interface to support different XR SDKs (Meta/Oculus, Unity XR, etc.)
    /// </summary>
    public interface IXRRigProvider
    {
        /// <summary>
        /// The transform representing the user's head (HMD)
        /// </summary>
        Transform HeadTransform { get; }

        /// <summary>
        /// The transform representing the user's left hand/controller
        /// </summary>
        Transform LeftHandTransform { get; }

        /// <summary>
        /// The transform representing the user's right hand/controller
        /// </summary>
        Transform RightHandTransform { get; }

        /// <summary>
        /// The root transform of the XR rig (used for offset calculations)
        /// </summary>
        Transform RigRootTransform { get; }

        /// <summary>
        /// Whether this XR provider supports finger tracking
        /// </summary>
        bool SupportsFingerTracking { get; }

        /// <summary>
        /// Gets the transforms for left hand finger bones.
        /// Returns empty list if finger tracking is not supported.
        /// </summary>
        IReadOnlyList<Transform> GetLeftFingerTransforms();

        /// <summary>
        /// Gets the transforms for right hand finger bones.
        /// Returns empty list if finger tracking is not supported.
        /// </summary>
        IReadOnlyList<Transform> GetRightFingerTransforms();

        /// <summary>
        /// Whether the XR provider has been initialized and is ready to use
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initialize the XR provider. Call this before using any other properties.
        /// </summary>
        void Initialize();
    }
}
