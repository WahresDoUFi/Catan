using GamePlay;
using Unity.Netcode;
using UnityEngine;

namespace Networking
{
    public class ConnectionApprovalManager : MonoBehaviour
    {
        public static ConnectionApprovalManager Instance;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }

        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = GameManager.Instance.PlayerCount < GameManager.MaxPlayers;
            response.Approved &= GameManager.Instance.State == GameManager.GameState.Waiting;
        }
    }
}