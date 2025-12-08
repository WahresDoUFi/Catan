using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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
            hostButton.onClick.AddListener(Host);
            joinButton.onClick.AddListener(Join);
        }

        private void Host()
        {
            StartCoroutine(WaitForHost());
        }

        private IEnumerator WaitForHost()
        {
            SetButtonsActive(false);
            var startHost = StartHostWithRelay(GameManager.MaxPlayers, "udp");
            while (!startHost.IsCompleted)
            {
                yield return null;
            }
            Debug.Log("Join code: " + startHost.Result);
            SetButtonsActive(true);
        }

        private void Join()
        {
            string code = joinCodeInput.text;
            if (string.IsNullOrEmpty(code)) return;
            StartCoroutine(WaitForClient(code));
        }

        private IEnumerator WaitForClient(string code)
        {
            SetButtonsActive(false);
            var staratClient = StartClientWithRelay(code, "upd");
            while (!staratClient.IsCompleted)
            {
                yield return null;
            }

            SetButtonsActive(true);
        }

        private void SetButtonsActive(bool active)
        {
            hostButton.interactable = active;
            joinButton.interactable = active;
        }
        
        private async Task<string> StartHostWithRelay(int maxConnections, string connectionType)
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(allocation.ToRelayServerData(connectionType));
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            if (NetworkManager.Singleton.StartHost())
            {
                NetworkManager.Singleton.SceneManager.LoadScene(boardSceneName, LoadSceneMode.Single);
                return joinCode;
            }
            return null;
        }
        
        private async Task<bool> StartClientWithRelay(string joinCode, string connectionType)
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(allocation.ToRelayServerData(connectionType));
            return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
        }
    }
}
