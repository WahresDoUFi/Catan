using Unity.Netcode;
using System;
using UnityEngine;

public class ConnectionNotificationManager : MonoBehaviour
    {
        public static ConnectionNotificationManager Instance;
        public event Action<ulong, ConnectionStatus> OnClientConnectionNotification;

        public enum ConnectionStatus
        {
            Connected,
            Disconnected,
        }
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkManager not found");
                return;
            }

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
        }

        private void OnClientConnectedCallback(ulong clientId)
        {
            OnClientConnectionNotification?.Invoke(clientId, ConnectionStatus.Connected);
        }
        
        private void OnClientDisconnectedCallback(ulong clientId)
        {
            OnClientConnectionNotification?.Invoke(clientId, ConnectionStatus.Disconnected);
        }
    }
