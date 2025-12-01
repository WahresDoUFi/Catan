using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestScriopt : MonoBehaviour
{
#if UNITY_EDITOR
    public UnityEditor.SceneAsset boardScene;
#endif
    [SerializeField] private string boardSceneName;

    private void OnValidate()
    {
        boardSceneName = boardScene.name;
    }

    void OnGUI()
    {
        if (!NetworkManager.Singleton) return;
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            StartButtons();
        }
        else
        {
            DisconnectButton();
        }

        GUILayout.EndArea();
    }

    void StartButtons()
    {
        if (GUILayout.Button("Host"))
        {
            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene(boardSceneName, LoadSceneMode.Single);
        }

        if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
        if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
    }

    void DisconnectButton()
    {
        if (GUILayout.Button("Disconnect")) NetworkManager.Singleton.Shutdown();
    }
}