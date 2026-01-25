using System;
using UnityEngine;
using Unity.Netcode;

namespace VRNetworkAvatar
{
    /// <summary>
    /// Handles spawning network avatars when clients connect.
    /// Attach to a GameObject in your network scene (typically alongside VRNetworkAvatarManager).
    /// </summary>
    public class NetworkAvatarSpawner : MonoBehaviour
    {
        [Header("Avatar Prefab")]
        [Tooltip("The avatar prefab to spawn for each player. Must have NetworkObject component.")]
        [SerializeField] private GameObject avatarPrefab;

        [Header("Spawn Settings")]
        [Tooltip("Automatically spawn avatar when client connects")]
        [SerializeField] private bool spawnOnConnect = true;

        [Header("Spawn Points (Optional)")]
        [Tooltip("If set, players spawn at these positions. Otherwise spawn at origin.")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private bool randomizeSpawnPoint = false;

        private int nextSpawnPointIndex = 0;

        /// <summary>
        /// Event fired before spawning an avatar. Return a different prefab to override.
        /// Parameters: clientId, defaultPrefab. Returns: prefab to spawn.
        /// </summary>
        public Func<ulong, GameObject, GameObject> OnSelectAvatarPrefab;

        /// <summary>
        /// Event fired after spawning an avatar. Use for additional setup.
        /// Parameters: clientId, spawnedAvatar
        /// </summary>
        public event Action<ulong, GameObject> OnAvatarSpawned;

        private void Start()
        {
            if (avatarPrefab == null)
            {
                // Try to get from config
                var manager = VRNetworkAvatarManager.Instance;
                if (manager != null && manager.Config != null && manager.Config.defaultAvatarPrefab != null)
                {
                    avatarPrefab = manager.Config.defaultAvatarPrefab;
                }
            }

            if (avatarPrefab == null)
            {
                Debug.LogError("[NetworkAvatarSpawner] No avatar prefab assigned!");
                return;
            }

            if (spawnOnConnect && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            SpawnAvatarForClient(clientId);
        }

        /// <summary>
        /// Spawn an avatar for the specified client.
        /// Can be called manually if spawnOnConnect is disabled.
        /// </summary>
        public void SpawnAvatarForClient(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogError("[NetworkAvatarSpawner] SpawnAvatarForClient can only be called on the server.");
                return;
            }

            // Select prefab (allow override via event)
            GameObject prefabToSpawn = avatarPrefab;
            if (OnSelectAvatarPrefab != null)
            {
                prefabToSpawn = OnSelectAvatarPrefab.Invoke(clientId, avatarPrefab);
            }

            if (prefabToSpawn == null)
            {
                Debug.LogError($"[NetworkAvatarSpawner] No prefab to spawn for client {clientId}!");
                return;
            }

            // Get spawn position
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform spawnPoint = GetNextSpawnPoint();
                if (spawnPoint != null)
                {
                    spawnPosition = spawnPoint.position;
                    spawnRotation = spawnPoint.rotation;
                }
            }

            // Instantiate and spawn
            GameObject avatarInstance = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);

            // Get identity component and spawn as player object
            var identity = avatarInstance.GetComponent<NetworkPlayerIdentity>();
            if (identity != null)
            {
                identity.SpawnAsPlayerObject(clientId);
            }
            else
            {
                // Fallback: spawn via NetworkObject directly
                var networkObject = avatarInstance.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.SpawnAsPlayerObject(clientId, true);
                    Debug.Log($"[NetworkAvatarSpawner] Spawned avatar for client {clientId}");
                }
                else
                {
                    Debug.LogError($"[NetworkAvatarSpawner] Avatar prefab missing NetworkObject component!");
                    Destroy(avatarInstance);
                    return;
                }
            }

            OnAvatarSpawned?.Invoke(clientId, avatarInstance);
        }

        private Transform GetNextSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return null;

            Transform spawnPoint;

            if (randomizeSpawnPoint)
            {
                int randomIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
                spawnPoint = spawnPoints[randomIndex];
            }
            else
            {
                spawnPoint = spawnPoints[nextSpawnPointIndex];
                nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Length;
            }

            return spawnPoint;
        }

        /// <summary>
        /// Set the avatar prefab at runtime.
        /// </summary>
        public void SetAvatarPrefab(GameObject prefab)
        {
            avatarPrefab = prefab;
        }

        /// <summary>
        /// Set spawn points at runtime.
        /// </summary>
        public void SetSpawnPoints(Transform[] points)
        {
            spawnPoints = points;
            nextSpawnPointIndex = 0;
        }
    }
}
