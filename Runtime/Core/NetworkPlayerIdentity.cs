using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;

namespace VRNetworkAvatar
{
    /// <summary>
    /// Handles player identity and display name synchronization across the network.
    /// Attach to the networked player avatar prefab.
    /// </summary>
    public class NetworkPlayerIdentity : NetworkBehaviour
    {
        [Header("Display Name")]
        [SerializeField] private TextMeshProUGUI displayNameText;

        [Header("Settings")]
        [SerializeField] private bool autoSubscribeOnSpawn = true;

        /// <summary>
        /// Network-synchronized display name. Readable by everyone, writable only by server.
        /// </summary>
        public NetworkVariable<FixedString64Bytes> DisplayName = new NetworkVariable<FixedString64Bytes>(
            "",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// The NetworkObject component for this player.
        /// </summary>
        public NetworkObject NetworkObj { get; private set; }

        private void Awake()
        {
            NetworkObj = GetComponent<NetworkObject>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (autoSubscribeOnSpawn)
            {
                SubscribeToDisplayName();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            DisplayName.OnValueChanged -= OnDisplayNameChanged;
        }

        /// <summary>
        /// Subscribe to display name changes and update UI.
        /// Call this manually if autoSubscribeOnSpawn is disabled.
        /// </summary>
        public void SubscribeToDisplayName()
        {
            DisplayName.OnValueChanged += OnDisplayNameChanged;
            UpdateDisplayNameUI(DisplayName.Value.ToString());
        }

        /// <summary>
        /// Set the display name for this player.
        /// Only the owner can request a name change.
        /// </summary>
        public void SetDisplayName(string name)
        {
            if (IsOwner)
            {
                UpdateDisplayNameServerRpc(name);
            }
            else
            {
                Debug.LogWarning("[NetworkPlayerIdentity] Only the owner can set the display name.");
            }
        }

        /// <summary>
        /// Get the current display name.
        /// </summary>
        public string GetDisplayName()
        {
            return DisplayName.Value.ToString();
        }

        [ServerRpc(RequireOwnership = false)]
        private void UpdateDisplayNameServerRpc(string name, ServerRpcParams rpcParams = default)
        {
            // Validate that the request comes from the owner
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
            {
                Debug.LogWarning($"[NetworkPlayerIdentity] Rejected name change request from non-owner client {rpcParams.Receive.SenderClientId}");
                return;
            }

            DisplayName.Value = name;
        }

        private void OnDisplayNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
        {
            UpdateDisplayNameUI(newValue.ToString());
        }

        private void UpdateDisplayNameUI(string name)
        {
            if (displayNameText != null)
            {
                displayNameText.text = name;
            }
        }

        /// <summary>
        /// Spawns this object as a player object for the specified client.
        /// Should only be called on the server.
        /// </summary>
        public void SpawnAsPlayerObject(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogError("[NetworkPlayerIdentity] SpawnAsPlayerObject can only be called on the server.");
                return;
            }

            NetworkObj.SpawnAsPlayerObject(clientId, true);
            Debug.Log($"[NetworkPlayerIdentity] Spawned player object for client {clientId}");
        }

        /// <summary>
        /// Despawns this player object from the network.
        /// Should only be called on the server.
        /// </summary>
        public void DespawnFromNetwork()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogError("[NetworkPlayerIdentity] DespawnFromNetwork can only be called on the server.");
                return;
            }

            NetworkObj.Despawn(true);
        }
    }
}
