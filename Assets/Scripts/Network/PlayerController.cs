using UnityEngine;
using Unity.Netcode;

namespace SpawningObject.Network
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float interpolationSpeed = 12f;

        [Header("Visuals")]
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Animator animator;

        [Header("Gameplay")]
        [SerializeField] private int maxHealth = 100;

        [Header("Respawn")]
        [SerializeField] private float respawnDelay = 3f;

        private void OnHealthChanged(int previous, int current)
        {
            if (current <= 0 && previous > 0)
            {
                IsDead = true;
                if (IsOwner && animator != null)
                    animator.SetTrigger("Die");

                if (IsServer)
                {
                    Invoke(nameof(ServerRespawn), respawnDelay);
                }
            }
            else if (current > 0 && previous <= 0)
            {
                IsDead = false;
            }
        }

        private void ServerRespawn()
        {
            if (!IsServer) return;
            if (!IsDead) return; // safety: don't respawn if somehow already alive

            Health.Value = maxHealth; // fires OnValueChanged -> IsDead = false, on every peer

            var spawnManager = FindObjectOfType<GameSpawnManager>();
            Transform point = spawnManager != null ? spawnManager.GetNextSpawnPoint() : null;
            Vector3 spawnPos = point != null ? point.position : transform.position;
            Quaternion spawnRot = point != null ? point.rotation : transform.rotation;

            _controller.enabled = false;
            transform.SetPositionAndRotation(spawnPos, spawnRot);
            _controller.enabled = true;

            _netPosition.Value = spawnPos;
            _netRotation.Value = spawnRot;

            RespawnClientRpc();
        }

        [ClientRpc]
        private void RespawnClientRpc()
        {
            if (animator != null)
                animator.SetTrigger("Respawn");
        }
        private readonly NetworkVariable<Vector3> _netPosition = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<Quaternion> _netRotation = new NetworkVariable<Quaternion>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<int> Health = new NetworkVariable<int>(
            100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public readonly NetworkVariable<int> Score = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public bool IsDead { get; private set; }

        private CharacterController _controller;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _netPosition.Value = transform.position;
                _netRotation.Value = transform.rotation;
                Health.Value = maxHealth;
            }

            Health.OnValueChanged += OnHealthChanged;

            if (bodyRenderer != null)
            {
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_Color", ColorForClientId(OwnerClientId));
                bodyRenderer.SetPropertyBlock(mpb);
            }
        }

        public override void OnNetworkDespawn()
        {
            Health.OnValueChanged -= OnHealthChanged;
        }

        private void Update()
        {
            if (IsOwner)
            {
                if (!IsDead)
                    HandleOwnerInput();
                else if (animator != null)
                    animator.SetFloat("Speed", 0f);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, _netPosition.Value, Time.deltaTime * interpolationSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, _netRotation.Value, Time.deltaTime * interpolationSpeed);
            }
        }

        private void HandleOwnerInput()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(h, 0f, v);

            if (inputDir.sqrMagnitude > 1f)
                inputDir.Normalize();

            if (animator != null)
                animator.SetFloat("Speed", inputDir.magnitude);

            if (inputDir.sqrMagnitude > 0.0001f)
            {
                Vector3 motion = inputDir * moveSpeed * Time.deltaTime;
                _controller.Move(motion);

                Quaternion targetRot = Quaternion.LookRotation(inputDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            SubmitMovementServerRpc(inputDir, transform.rotation);
        }

        [ServerRpc]
        private void SubmitMovementServerRpc(Vector3 inputDir, Quaternion desiredRotation, ServerRpcParams rpcParams = default)
        {
            if (IsDead) return;

            if (inputDir.sqrMagnitude > 1f)
                inputDir.Normalize();

            Vector3 motion = inputDir * moveSpeed * Time.deltaTime;
            _controller.Move(motion);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);

            _netPosition.Value = transform.position;
            _netRotation.Value = transform.rotation;
        }

        /// <summary>Server-only: apply damage to this player. Returns true if this hit killed them.</summary>
        public bool ApplyDamage(int amount)
        {
            if (!IsServer || IsDead) return false;

            Health.Value = Mathf.Max(0, Health.Value - amount);

            if (Health.Value <= 0)
            {
                IsDead = true;
                return true;
            }
            return false;
        }

        private static Color ColorForClientId(ulong id)
        {
            Random.State prev = Random.state;
            Random.InitState((int)id * 9973 + 17);
            Color c = Color.HSVToRGB(Random.value, 0.65f, 0.95f);
            Random.state = prev;
            return c;
        }
    }
}