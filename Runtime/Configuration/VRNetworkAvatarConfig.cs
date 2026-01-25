using UnityEngine;

namespace VRNetworkAvatar
{
    /// <summary>
    /// Main configuration ScriptableObject for VRNetworkAvatar module.
    /// Create instances via Assets > Create > VRNetworkAvatar > Configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "VRNetworkAvatarConfig", menuName = "VRNetworkAvatar/Configuration", order = 0)]
    public class VRNetworkAvatarConfig : ScriptableObject
    {
        [Header("Avatar Settings")]
        [Tooltip("Default avatar prefab to spawn for players.")]
        public GameObject defaultAvatarPrefab;

        [Tooltip("Sync settings for avatar movement thresholds and constraints.")]
        public AvatarSyncSettings syncSettings;

        [Header("Local Avatar")]
        [Tooltip("Layer name for local player's avatar (used to hide from local camera if needed).")]
        public string localAvatarLayer = "LocalAvatar";

        [Tooltip("Disable colliders on local avatar to prevent self-collision.")]
        public bool disableLocalColliders = true;

        [Header("Spawning")]
        [Tooltip("Automatically spawn avatar when client connects.")]
        public bool spawnOnConnect = true;

        [Tooltip("Destroy avatar when client disconnects.")]
        public bool despawnOnDisconnect = true;

        [Header("XR Platform")]
        [Tooltip("Preferred XR platform. Auto will try to detect.")]
        public XRPlatformPreference xrPlatform = XRPlatformPreference.Auto;

        [Header("Debug")]
        [Tooltip("Enable verbose logging for debugging.")]
        public bool enableDebugLogging = false;
    }

    /// <summary>
    /// Preferred XR platform for avatar tracking.
    /// </summary>
    public enum XRPlatformPreference
    {
        /// <summary>
        /// Automatically detect XR platform.
        /// </summary>
        Auto,

        /// <summary>
        /// Force Meta/Oculus SDK (OVRRigProvider).
        /// </summary>
        MetaOculus,

        /// <summary>
        /// Force Unity XR Interaction Toolkit (GenericXRRigProvider).
        /// </summary>
        UnityXR
    }
}
