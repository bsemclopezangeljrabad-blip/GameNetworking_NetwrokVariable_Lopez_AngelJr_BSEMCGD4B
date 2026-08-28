using UnityEngine;
using UnityEngine.UI;

namespace SpawningObject.Network
{
  
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private int maxHealth = 100;

        private PlayerController _playerController;
        private Camera _mainCamera;

        private void Start()
        {
            _playerController = GetComponentInParent<PlayerController>();
            _mainCamera = Camera.main;

            if (_playerController != null)
            {
                _playerController.Health.OnValueChanged += OnHealthChanged;
                UpdateSlider(_playerController.Health.Value);
            }
        }

        private void OnDestroy()
        {
            if (_playerController != null)
                _playerController.Health.OnValueChanged -= OnHealthChanged;
        }

        private void LateUpdate()
        {
            // Billboard: always face the local camera.
            if (_mainCamera != null)
            {
                transform.forward = _mainCamera.transform.forward;
            }
        }

        private void OnHealthChanged(int previous, int current)
        {
            UpdateSlider(current);
        }

        private void UpdateSlider(int current)
        {
            if (healthSlider != null)
                healthSlider.value = (float)current / maxHealth;
        }
    }
}