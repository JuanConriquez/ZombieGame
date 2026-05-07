using UnityEngine;
using Unity.Netcode;

public class NetworkGameManager : NetworkBehaviour
{
    public Transform player1Spawn;
    public Transform player2Spawn;

    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUI.Button(new Rect(10, 10, 120, 40), "Host Game"))
            {
                NetworkManager.Singleton.StartHost();
            }
            if (GUI.Button(new Rect(10, 60, 120, 40), "Join Game"))
            {
                NetworkManager.Singleton.StartClient();
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.OnClientConnectedCallback += OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        if (clientId == 1)
        {
            foreach(var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId == 1)
                {
                    client.PlayerObject.transform.position = player2Spawn.position;
                }
                else
                {
                    client.PlayerObject.transform.position = player1Spawn.position;
                }
            }
        }
    }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
