using Unity.Netcode;
using UnityEngine;

public class TestScriopt : MonoBehaviour
{
    private NetworkManager _networkManager;
    [SerializeField]
    private MapGenerator _mapGenerator;

    void Awake()
    {
        _networkManager = GetComponent<NetworkManager>();
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!_networkManager.IsClient && !_networkManager.IsServer)
        {
            StartButtons();
        }

        GUILayout.EndArea();
    }

    void StartButtons()
    {
        if (GUILayout.Button("Host"))
        {
            _networkManager.OnServerStarted += _mapGenerator.InitializeMap;
            _networkManager.StartHost();
        }
        if (GUILayout.Button("Client")) _networkManager.StartClient();
        if (GUILayout.Button("Server")) _networkManager.StartServer();
    }
}
