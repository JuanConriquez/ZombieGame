using UnityEngine;
using Unity.Netcode;

public class NetworkGameManager : NetworkBehaviour
{
    public Transform player1Spawn;
    public Transform player2Spawn;

    private WaveManager waveManager;

    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUI.Button(new Rect(10, 10, 120, 40), "Host Game"))
                NetworkManager.Singleton.StartHost();

            if (GUI.Button(new Rect(10, 60, 120, 40), "Join Game"))
                NetworkManager.Singleton.StartClient();
        }
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.OnClientConnectedCallback += OnClientConnected;

        // listen for wave countdown so we can respawn dead players between waves
        waveManager = FindAnyObjectByType<WaveManager>();
        if (waveManager != null)
            waveManager.OnWaveStarted += OnWaveStarted;
    }

    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        if (clientId == 1)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId == 1)
                    client.PlayerObject.transform.position = player2Spawn.position;
                else
                    client.PlayerObject.transform.position = player1Spawn.position;
            }
        }
    }

    // fires at the start of every new wave — respawn all players at their spawn points
    void OnWaveStarted(int wave)
    {
        if (!IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            PlayerMovement pm = client.PlayerObject.GetComponent<PlayerMovement>();
            if (pm == null) continue;

            if (!pm.IsDead) continue;

            // player 1 is client 0 (host), player 2 is client 1
            Vector3 spawnPos = client.ClientId == 0 ? player1Spawn.position : player2Spawn.position;
            pm.Respawn(spawnPos);
        }
    }
}
