using Unity.Netcode;
using UnityEngine;

public class SetUp : MonoBehaviour
{
    [SerializeField] GameObject networkManagerPrefab;
    private void Start()
    {
        if (NetworkManager.Singleton) return;
        var networkManager = Instantiate(networkManagerPrefab).GetComponent<NetworkManager>();
        networkManager.SetSingleton();
    }
}