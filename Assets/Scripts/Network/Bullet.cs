using SpawningObject.Network;
using Unity.Netcode;
using UnityEngine;

namespace SpawningObject.Network
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Bullet : NetworkBehaviour
    {
        [SerializeField] private float lifeSeconds = 5f;

        private ulong _shooterClientId;
        private int _damage;
        private bool _consumed;

        /// <summary>Server-only: called right after Instantiate, before Spawn.</summary>
        public void Initialize(ulong shooterClientId, int damage)
        {
            _shooterClientId = shooterClientId;
            _damage = damage;
        }

        public override void OnNetworkSpawn()
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = !IsServer; // only the server actually simulates movement
            }

            if (IsServer)
            {
                Invoke(nameof(ExpireIfStillAlive), lifeSeconds);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || _consumed) return;

            var victim = other.GetComponent<PlayerController>();
            if (victim == null) return;
            if (victim.OwnerClientId == _shooterClientId) return; // don't hit yourself

            _consumed = true;

            bool killed = victim.ApplyDamage(_damage);

            if (killed)
            {
                AwardScoreToShooter();
            }

            DespawnSelf();
        }

        private void AwardScoreToShooter()
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(_shooterClientId, out var client)) return;
            if (client.PlayerObject == null) return;

            var shooterController = client.PlayerObject.GetComponent<PlayerController>();
            if (shooterController != null)
            {
                shooterController.Score.Value += 1;
            }
        }

        private void ExpireIfStillAlive()
        {
            if (_consumed) return;
            _consumed = true;
            DespawnSelf();
        }

        private void DespawnSelf()
        {
            if (!IsServer) return;
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
    }
}