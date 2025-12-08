using System;
using System.Collections;
using System.Threading.Tasks;
using GamePlay;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Networking
{
    public class MatchmakingManager : MonoBehaviour
    {
        
#if UNITY_EDITOR
        public UnityEditor.SceneAsset boardScene;
    
        private void OnValidate()
        {
            boardSceneName = boardScene.name;
        }
#endif
        [SerializeField] private string boardSceneName;

        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private TMP_InputField joinCodeInput;
        
        private void Awake()
        {
            hostButton.onClick.AddListener(() => Host());
            joinButton.onClick.AddListener(() => Join());
        }

        private IEnumerator Start()
        {
            SetButtonsActive(false);
            yield return UnityServices.InitializeAsync();
            yield return new WaitForSeconds(0.5f);
            yield return AuthenticationService.Instance.SignInAnonymouslyAsync();
            SetButtonsActive(true);
        }

        private async Task Host()
        {
            SetButtonsActive(false);
            string startHost = await StartHostWithRelay(GameManager.MaxPlayers, "dtls");
            Debug.Log("Join code: " + startHost);
            SetButtonsActive(true);
        }

        private async Task Join()
        {
            SetButtonsActive(false);
            string code = joinCodeInput.text;
            if (string.IsNullOrEmpty(code)) return;
            if (!await StartClientWithRelay(code, "dtls"))
                Debug.LogWarning("Could not start client");
            SetButtonsActive(true);
        }

        private void SetButtonsActive(bool active)
        {
            hostButton.interactable = active;
            joinButton.interactable = active;
        }
        
        private async Task<string> StartHostWithRelay(int maxConnections, string connectionType)
        {
            try
            {
                var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                NetworkManager.Singleton.GetComponent<UnityTransport>()
                    .SetRelayServerData(allocation.ToRelayServerData(connectionType));
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                if (NetworkManager.Singleton.StartHost())
                {
                    NetworkManager.Singleton.SceneManager.LoadScene(boardSceneName, LoadSceneMode.Single);
                    return joinCode;
                }

                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not start host: " + e.Message);
            }
            return null;
        }
        
        private async Task<bool> StartClientWithRelay(string joinCode, string connectionType)
        {
            Debug.Log("Searching for relay allocation");
            JoinAllocation allocation = null;
            try
            {
                allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            }
            catch (Exception e)
            {
                Debug.Log("Could not find allocation: " + e.Message);
                return false;
            }
            Debug.Log(allocation);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(allocation.ToRelayServerData(connectionType));
            Debug.Log("Starting client");
            return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
        }
    }
}
