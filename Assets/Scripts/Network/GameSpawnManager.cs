using UnityEngine;
using Unity.Netcode;

namespace SpawningObject.Network
{
    public class GameSpawnManager : MonoBehaviour
    {
        [SerializeField] private Transform[] playerSpawnPoints;

        private int _nextSpawnIndex;

        private void OnEnable()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
            var playerObject = client.PlayerObject;
            if (playerObject == null) return;

            Transform point = GetNextSpawnPoint();
            if (point == null) return;

            if (playerObject.TryGetComponent<CharacterController>(out var cc))
            {
                cc.enabled = false;
                playerObject.transform.SetPositionAndRotation(point.position, point.rotation);
                cc.enabled = true;
            }
            else
            {
                playerObject.transform.SetPositionAndRotation(point.position, point.rotation);
            }
        }

        /// <summary>Server-only: round-robin spawn point, usable for both initial spawn and respawn.</summary>
        public Transform GetNextSpawnPoint()
        {
            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0) return null;
            Transform point = playerSpawnPoints[_nextSpawnIndex % playerSpawnPoints.Length];
            _nextSpawnIndex++;
            return point;
        }
    }
}