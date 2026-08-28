using SpawningObject.Network;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace SpawningObject.Network
{

    public class ScoreboardUI : MonoBehaviour
    {
        [SerializeField] private Text scoreText;
        [SerializeField] private float refreshInterval = 0.5f;

        private float _timer;

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < refreshInterval) return;
            _timer = 0f;

            Refresh();
        }

        private void Refresh()
        {
            if (NetworkManager.Singleton == null || scoreText == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("Scoreboard");

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null) continue;
                var pc = client.PlayerObject.GetComponent<PlayerController>();
                if (pc == null) continue;

                sb.AppendLine($"Player {client.ClientId}: {pc.Score.Value} pts  (HP {pc.Health.Value})");
            }

            scoreText.text = sb.ToString();
        }
    }
}