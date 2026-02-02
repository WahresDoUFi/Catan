using System;
using System.Security.Cryptography;
using Unity.Netcode;
using UnityEngine;
using User;

namespace GamePlay
{
    public class Bandit : NetworkBehaviour
    {
        public static Bandit Instance { get; private set; }

        public event Action<MapTile> BanditMoved;

        [SerializeField] private LayerMask tileLayer;

        readonly NetworkVariable<NetworkBehaviourReference> _tileId = new();
        private Renderer[] _renderers;

        private void Awake()
        {
            Instance = this;
            _renderers = GetComponentsInChildren<Renderer>();
            SetVisible(false);
        }

        public override void OnNetworkSpawn()
        {
            _tileId.OnValueChanged += TileIdChanged;
        }

        public void SetInitialTile(MapTile desert)
        {
            if (_tileId.Value.TryGet(out _)) return;
            if (NetworkManager.IsHost == false) return;

            _tileId.Value = desert;
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void ClickPerformed()
        {
            if (!GameManager.Instance.RepositionBandit || !GameManager.Instance.IsMyTurn()) return;
            if (GameManager.Instance.CardsToDiscard > 0) return;
            var go = CameraController.Instance.Raycast(tileLayer);
            if (go == null) return;
            if (!go.TryGetComponent<MapTile>(out var tile)) return;
            GameManager.Instance.SetBanditTile(tile);
        }

        public void SetTargetTile(MapTile tile)
        {
            if (NetworkManager.IsHost)
                _tileId.Value = tile;
        }

        private void SetVisible(bool visible)
        {
            foreach (var renderer in _renderers)
            {
                renderer.enabled = visible;
            }
        }

        private void TileIdChanged(NetworkBehaviourReference previousValue, NetworkBehaviourReference newValue)
        {
            if (!newValue.TryGet(out var tileObject)) return;
            var tile = tileObject.GetComponent<MapTile>();
            var targetTransform = tile.BanditPosition ?? tile.transform;
            transform.position = targetTransform.position;
            transform.rotation = targetTransform.rotation;
            BanditMoved?.Invoke(tile);
            SetVisible(tile.Discovered);
        }
    }
}