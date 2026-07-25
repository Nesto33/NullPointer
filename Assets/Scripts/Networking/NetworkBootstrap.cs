using Unity.Netcode;
using UnityEngine;

namespace RiverDeutsch.Networking
{
    /// <summary>
    /// Spawns the single authoritative <see cref="NetworkGameSession"/> as soon as the
    /// server (or host) comes online. Attach to a persistent GameObject alongside the
    /// NetworkManager and assign the NetworkGameSession prefab in the inspector.
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public class NetworkBootstrap : MonoBehaviour
    {
        [SerializeField] private NetworkGameSession sessionPrefab;

        private NetworkManager networkManager;

        private void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
        }

        private void OnEnable()
        {
            networkManager.OnServerStarted += SpawnSession;
        }

        private void OnDisable()
        {
            networkManager.OnServerStarted -= SpawnSession;
        }

        private void SpawnSession()
        {
            if (sessionPrefab == null)
            {
                Debug.LogError("NetworkBootstrap: no NetworkGameSession prefab assigned.");
                return;
            }

            NetworkGameSession instance = Instantiate(sessionPrefab);
            instance.NetworkObject.Spawn();
        }
    }
}
