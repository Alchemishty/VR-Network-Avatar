using UnityEngine;
using Unity.Netcode.Components;

namespace VRNetworkAvatar
{
    /// <summary>
    /// Client-authoritative NetworkTransform for VR avatar synchronization.
    /// The owning client controls the transform, which is then synced to other clients.
    ///
    /// This is essential for VR avatars where the client tracks the physical movement
    /// of the headset and controllers.
    ///
    /// Note: Client authority means trusting the client to report accurate positions.
    /// For security-sensitive games, consider server-side validation.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientAuthorityNetworkTransform : NetworkTransform
    {
        /// <summary>
        /// Override to make this transform client-authoritative instead of server-authoritative.
        /// </summary>
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
