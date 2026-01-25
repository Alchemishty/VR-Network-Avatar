using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace VRNetworkAvatar
{
    /// <summary>
    /// Central manager for VR network avatars.
    /// Combines client tracking (PlayersManager) and avatar registry (ClientPlayerAvatars) functionality.
    /// Should be a singleton in the scene, typically on a networked object.
    /// </summary>
    public class VRNetworkAvatarManager : NetworkBehaviour
    {
        private static VRNetworkAvatarManager instance;
        public static VRNetworkAvatarManager Instance => instance;

        [Header("Configuration")]
        [SerializeField] private VRNetworkAvatarConfig config;

        // Connected client tracking (server-side)
        private HashSet<ulong> connectedClientIds = new HashSet<ulong>();

        // Avatar registry (all clients)
        private Dictionary<ulong, GameObject> clientAvatars = new Dictionary<ulong, GameObject>();

        // Local player's avatar reference
        private GameObject localPlayerAvatar;
        public GameObject LocalPlayerAvatar => localPlayerAvatar;

        // Events for client connections
        public event Action<ulong> OnClientConnected;
        public event Action<ulong> OnClientDisconnected;

        // Events for avatar lifecycle
        public event Action<ulong, GameObject> OnAvatarRegistered;
        public event Action<ulong, GameObject> OnAvatarUnregistered;

        // Generic events for extensions (e.g., emoji, speech indicators)
        public event Action<ulong, int> OnAvatarEvent;

        public VRNetworkAvatarConfig Config => config;
        public IReadOnlyCollection<ulong> ConnectedClientIds => connectedClientIds;
        public IReadOnlyDictionary<ulong, GameObject> ClientAvatars => clientAvatars;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            // Optionally make persistent across scenes
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                // Check if server is already started
                if (NetworkManager.Singleton.IsServer)
                {
                    OnServerStarted();
                }
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            }
            else
            {
                Debug.LogError("[VRNetworkAvatarManager] NetworkManager.Singleton is null. Check scene setup.");
            }
        }

        private void OnServerStarted()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

            // Add any clients that are already connected
            var connectedClients = NetworkManager.Singleton.ConnectedClientsIds;
            if (connectedClients != null)
            {
                foreach (ulong clientId in connectedClients)
                {
                    AddClientId(clientId);
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnect;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        #region Client ID Management (Server-side)

        private void HandleClientConnect(ulong clientId)
        {
            Debug.Log($"[VRNetworkAvatarManager] Client connected: {clientId}");
            AddClientId(clientId);
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            Debug.Log($"[VRNetworkAvatarManager] Client disconnected: {clientId}");
            RemoveClientId(clientId, hasDisconnected: true);
        }

        public void AddClientId(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (!connectedClientIds.Contains(clientId))
            {
                connectedClientIds.Add(clientId);
                Debug.Log($"[VRNetworkAvatarManager] Added Client ID {clientId}");
                OnClientConnected?.Invoke(clientId);
            }
        }

        public void RemoveClientId(ulong clientId, bool hasDisconnected = false)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (connectedClientIds.Remove(clientId))
            {
                Debug.Log($"[VRNetworkAvatarManager] Removed Client ID {clientId}");

                // Handle avatar cleanup
                if (hasDisconnected)
                {
                    // Client disconnected, avatar will be auto-despawned by NGO
                    RemoveAvatarFromRegistry(clientId);
                }
                else
                {
                    // Manual removal, need to despawn avatar
                    RemoveAndDespawnAvatar(clientId);
                }

                OnClientDisconnected?.Invoke(clientId);
            }
            else
            {
                Debug.LogWarning($"[VRNetworkAvatarManager] Client ID {clientId} not found for removal.");
            }
        }

        public bool IsClientConnected(ulong clientId)
        {
            return connectedClientIds.Contains(clientId);
        }

        public List<ulong> GetAllClientIds()
        {
            return new List<ulong>(connectedClientIds);
        }

        #endregion

        #region Avatar Registry (All clients)

        public void RegisterAvatar(GameObject avatarObject)
        {
            var networkObject = avatarObject.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogError("[VRNetworkAvatarManager] Cannot register avatar without NetworkObject component.");
                return;
            }

            ulong clientId = networkObject.OwnerClientId;

            if (!clientAvatars.ContainsKey(clientId))
            {
                clientAvatars.Add(clientId, avatarObject);
                Debug.Log($"[VRNetworkAvatarManager] Registered avatar for client {clientId}");

                // Track local player's avatar
                if (networkObject.IsOwner)
                {
                    localPlayerAvatar = avatarObject;
                }

                OnAvatarRegistered?.Invoke(clientId, avatarObject);
            }
        }

        public void UnregisterAvatar(ulong clientId)
        {
            if (clientAvatars.TryGetValue(clientId, out var avatar))
            {
                clientAvatars.Remove(clientId);
                Debug.Log($"[VRNetworkAvatarManager] Unregistered avatar for client {clientId}");

                if (localPlayerAvatar == avatar)
                {
                    localPlayerAvatar = null;
                }

                OnAvatarUnregistered?.Invoke(clientId, avatar);
            }
        }

        private void RemoveAvatarFromRegistry(ulong clientId)
        {
            UnregisterAvatar(clientId);
        }

        private void RemoveAndDespawnAvatar(ulong clientId)
        {
            if (clientAvatars.TryGetValue(clientId, out var avatar))
            {
                var networkObject = avatar.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn(true);
                }
                UnregisterAvatar(clientId);
            }
        }

        public GameObject GetAvatar(ulong clientId)
        {
            if (clientAvatars.TryGetValue(clientId, out var avatar))
            {
                return avatar;
            }
            Debug.LogWarning($"[VRNetworkAvatarManager] No avatar found for client {clientId}");
            return null;
        }

        public T GetAvatarComponent<T>(ulong clientId) where T : Component
        {
            var avatar = GetAvatar(clientId);
            return avatar != null ? avatar.GetComponent<T>() : null;
        }

        #endregion

        #region Generic Avatar Events (RPCs)

        /// <summary>
        /// Send a generic event to all clients for a specific avatar.
        /// Use this for extensions like emoji reactions, speech indicators, etc.
        /// </summary>
        public void BroadcastAvatarEvent(int eventId)
        {
            BroadcastAvatarEventServerRpc(eventId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void BroadcastAvatarEventServerRpc(int eventId, ServerRpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            ReceiveAvatarEventClientRpc(senderClientId, eventId);
        }

        [ClientRpc]
        private void ReceiveAvatarEventClientRpc(ulong clientId, int eventId)
        {
            OnAvatarEvent?.Invoke(clientId, eventId);
        }

        #endregion
    }
}
