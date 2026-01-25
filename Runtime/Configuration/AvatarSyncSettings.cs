using UnityEngine;

namespace VRNetworkAvatar
{
    /// <summary>
    /// ScriptableObject containing synchronization settings for VR avatars.
    /// Create instances via Assets > Create > VRNetworkAvatar > Sync Settings.
    /// </summary>
    [CreateAssetMenu(fileName = "AvatarSyncSettings", menuName = "VRNetworkAvatar/Sync Settings", order = 1)]
    public class AvatarSyncSettings : ScriptableObject
    {
        [Header("Position & Rotation Thresholds")]
        [Tooltip("Minimum position change (squared) before syncing. Lower = more accurate but more network traffic.")]
        [Range(0.0001f, 0.01f)]
        public float positionThreshold = 0.001f;

        [Tooltip("Minimum rotation change (degrees) before syncing. Lower = more accurate but more network traffic.")]
        [Range(0.01f, 1f)]
        public float rotationThreshold = 0.1f;

        [Header("Physical Constraints")]
        [Tooltip("Maximum distance hands can be from body (arm length constraint).")]
        [Range(0.3f, 1.5f)]
        public float maxArmLength = 0.7f;

        [Tooltip("Maximum head tilt angle (degrees) in X and Z axes.")]
        [Range(15f, 90f)]
        public float headRotationClampAngle = 45f;

        [Header("Finger Tracking")]
        [Tooltip("Enable finger tracking synchronization.")]
        public bool enableFingerTracking = true;

        [Tooltip("Minimum rotation change (degrees) before syncing fingers.")]
        [Range(0.01f, 1f)]
        public float fingerRotationThreshold = 0.1f;

        [Header("Network Optimization")]
        [Tooltip("Interpolation speed for remote avatars. Higher = more responsive but potentially jittery.")]
        [Range(5f, 30f)]
        public float interpolationSpeed = 15f;

        [Tooltip("Extrapolation limit in seconds. How far ahead to predict movement.")]
        [Range(0f, 0.5f)]
        public float extrapolationLimit = 0.1f;
    }
}
