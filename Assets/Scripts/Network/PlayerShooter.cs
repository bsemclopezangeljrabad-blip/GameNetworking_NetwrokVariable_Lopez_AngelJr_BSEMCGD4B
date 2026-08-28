using UnityEngine;
using Unity.Netcode;

namespace SpawningObject.Network
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerShooter : NetworkBehaviour
    {
        [Header("Setup")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject bulletPrefab;

        [Header("Firing")]
        [SerializeField] private float fireRate = 0.3f;
        [SerializeField] private float bulletSpeed = 30f;

        private PlayerController _playerController;
        private float _nextFireTime;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (!IsOwner) return;
            if (_playerController.IsDead) return;
            if (Time.time < _nextFireTime) return;

            if (Input.GetButtonDown("Fire1")) // left mouse button by default
            {
                _nextFireTime = Time.time + fireRate;
                Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up;
                Vector3 direction = muzzle != null ? muzzle.forward : transform.forward;

                RequestFireServerRpc(origin, direction);
            }
        }

        [ServerRpc]
        private void RequestFireServerRpc(Vector3 origin, Vector3 direction, ServerRpcParams rpcParams = default)
        {
            if (_playerController.IsDead) return;
            if (bulletPrefab == null) return;

            GameObject instance = Instantiate(bulletPrefab, origin, Quaternion.LookRotation(direction));

            var bullet = instance.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.Initialize(shooterClientId: OwnerClientId, damage: 20);
            }

            if (instance.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = direction.normalized * bulletSpeed;
            }

            var networkObject = instance.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogError("[PlayerShooter] bulletPrefab is missing a NetworkObject component.");
                Destroy(instance);
                return;
            }

            networkObject.Spawn(destroyWithScene: true);
        }
    }
}