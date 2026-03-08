using System;
using GamePlay;
using Unity.Netcode;
using UnityEngine;
using User;

namespace Networking
{
    public class ConnectionApprovalManager : MonoBehaviour
    {
        private void Start()
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }

        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            var guid = BitConverter.ToString(request.Payload);

            if (GameManager.Instance)
            {
                if (NetworkManager.Singleton.ConnectedClients.Count >= GameManager.MaxPlayers)
                {
                    response.Approved = false;
                    response.Reason = "The lobby is full.";
                    return;
                }

                if (GameManager.Instance.State != GameManager.GameState.Waiting &&
                    !GameManager.Instance.PlayerGuidExists(guid))
                {
                    response.Approved = false;
                    response.Reason = "The game has already started.";
                    return;
                }
            }
            
            response.Approved = true;
            response.CreatePlayerObject = !GameManager.Instance || GameManager.Instance.State == GameManager.GameState.Waiting;
            GameManager.SetPlayerGuid(request.ClientNetworkId, guid);
        }
    }
}