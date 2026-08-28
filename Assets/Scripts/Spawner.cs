using Unity.Netcode;
using UnityEngine;

public class Spawner : NetworkBehaviour
{
    [SerializeField] private GameObject cubePrefab;
    private GameObject spawnedInstance;

    // Tawagin mo ito sa Server/Host lang (hal. via UI button o key press)
    public void SpawnCube()
    {
        if (!IsServer) return; // dapat server/host lang ang mag-spawn

        // 1. Instantiate
        spawnedInstance = Instantiate(cubePrefab, new Vector3(0, 1, 0), Quaternion.identity);

        // 2. Configure (halimbawa lang, pwede kang maglagay ng ibang setup dito)
        spawnedInstance.name = "SpawnedNetworkSphere";

        // 3. Network Spawn
        spawnedInstance.GetComponent<NetworkObject>().Spawn(true);
    }

    public void DespawnCube()
    {
        if (!IsServer) return;

        if (spawnedInstance != null)
        {
            spawnedInstance.GetComponent<NetworkObject>().Despawn(true); // true = destroy din
            spawnedInstance = null;
        }
    }

    private void Update()
    {
        // Simpleng paraan para ma-trigger: pindutin ang keyboard keys
        if (!IsServer) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnCube();
        }
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            DespawnCube();
        }
    }
}