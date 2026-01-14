using System;
using System.Collections;
using System.Linq;
using TMPro;
using UI.DevelopmentCards;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using User;

namespace GamePlay
{
    public class BuildManager : MonoBehaviour
    {
        public const float MaxCursorDistanceFromBuilding = 10f;

        public enum BuildType
        {
            Street,
            Settlement,
            City,
            DevelopmentCard
        }

        [Serializable]
        public struct BuildCosts
        {
            public string name;
            public BuildType type;
            public ResourceCosts[] costs;
        }

        [Serializable]
        public struct ResourceCosts : INetworkSerializable, IEquatable<ResourceCosts>
        {
            public Tile resource;
            public byte amount;
        
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref resource);
                serializer.SerializeValue(ref amount);
            }

            public bool Equals(ResourceCosts other)
            {
                return resource == other.resource && amount == other.amount;
            }

            public override bool Equals(object obj)
            {
                return obj is ResourceCosts other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine((int)resource, amount);
            }
        }

        private static BuildManager _instance;
        public static bool BuildModeActive => _instance._buildModeActive;
        public static BuildType ActiveBuildType => _instance._buildType;

        [SerializeField] private BuildCosts[] costs;

        [Header("BuildMode Display")]
        [SerializeField] private RectTransform buildModeDisplay;
        [SerializeField] private TextMeshProUGUI buildModeText;
        [SerializeField] private float buildModeExtraWidth;
        [SerializeField] private Button cancelButton;

        [Header("Music")]
        [SerializeField] private VolumeController buildModeMusic;
        [SerializeField] private float defaultVolume = 0.1f;
        [SerializeField] private float fadeTimeActive = 0.5f;
        [SerializeField] private float activeVolume = 0.4f;
        [SerializeField] private float fadeTimeDefault = 3f;

        private bool _buildModeActive;
        private BuildType _buildType;
        private Camera _mainCam;

        private void Awake()
        {
            _instance = this;
            cancelButton.onClick.AddListener(() => SetActive(false));
            _mainCam = Camera.main;
            SetActive(false);
        }

        private void Update()
        {
            if (_buildModeActive)
            {
                if (CameraController.IsOverview && GameManager.Instance.IsMyTurn()) HandleBuildingPreview();
                else SetActive(false);
            }
        }

        public static ResourceCosts[] GetCostsForBuilding(BuildType type)
        {
            return _instance.costs.FirstOrDefault(buildCost => buildCost.type == type).costs;
        }

        public void StartBuildStreet()
        {
            SelectBuildingType(BuildType.Street);
        }

        public void StartBuildSettlement()
        {
            SelectBuildingType(BuildType.Settlement);
        }

        public static void SetActive(bool active)
        {
            _instance.cancelButton.gameObject.SetActive(!Player.LocalPlayer.HasFreeBuildings());
            _instance._buildModeActive = active;
            _instance.buildModeDisplay.gameObject.SetActive(active);
            if (active)
            {
                CameraController.Instance.EnterOverview();
                _instance.StopAllCoroutines();
                _instance.StartCoroutine(_instance.FadeVolume(_instance.activeVolume, _instance.fadeTimeActive));
            }
            else
            {
                _instance.StopAllCoroutines();
                _instance.StartCoroutine(_instance.FadeVolume(_instance.defaultVolume, _instance.fadeTimeDefault));
            }
        }

        public static void SelectBuildingType(BuildType buildType)
        {
            if (DevelopmentCardsDisplay.HasToRevealCard) return;
            _instance._buildType = buildType;
            _instance.buildModeText.text = GetNameForBuildType(buildType);
            var size = _instance.buildModeDisplay.sizeDelta;
            size.x = _instance.buildModeText.GetPreferredValues().x + _instance.buildModeExtraWidth;
            _instance.buildModeDisplay.sizeDelta = size;
            SetActive(true);
        }

        public static void ShowInfoText(string text)
        {
            _instance.buildModeDisplay.gameObject.SetActive(true);
            _instance.buildModeText.text = text;
            var size = _instance.buildModeDisplay.sizeDelta;
            size.x = _instance.buildModeText.GetPreferredValues().x + _instance.buildModeExtraWidth;
            _instance.buildModeDisplay.sizeDelta = size;
            _instance.cancelButton.gameObject.SetActive(false);
        }

        public static void ConfirmPosition()
        {
            if (!BuildModeActive) return;
            if (_instance._buildType == BuildType.Street)
                _instance.PlaceStreet();
            else if (_instance._buildType == BuildType.Settlement)
                _instance.PlaceSettlement();
        }

        private static string GetNameForBuildType(BuildType buildType)
        {
            return _instance.costs.FirstOrDefault(cost => cost.type == buildType).name;
        }

        private void PlaceSettlement()
        {
            if (GameManager.Instance.PlaceSettlement(
                    Settlement.GetClosestSettlementTo(CameraController.Instance.MouseWorldPosition())))
                SetActive(false);
        }

        private void PlaceStreet()
        {
            if (GameManager.Instance.PlaceStreet(Street.GetClosestStreetTo(CameraController.Instance.MouseWorldPosition())))
                SetActive(false);
        }

        private void HandleBuildingPreview()
        {
            var worldPoint = CameraController.Instance.MouseWorldPosition();
            if (_buildType == BuildType.Street)
                HandleStreetPlacing(worldPoint);
            else if (_buildType == BuildType.Settlement)
                HandleSettlementPlacing(worldPoint);
        }

        private void HandleStreetPlacing(Vector3 worldPoint)
        {
            var street = Street.GetClosestStreetTo(worldPoint);
            if (street)
                street.Preview = true;
        }

        private void HandleSettlementPlacing(Vector3 worldPoint)
        {
            var settlement = Settlement.GetClosestSettlementTo(worldPoint);
            if (settlement)
                settlement.Preview = true;
        }

        private IEnumerator FadeVolume(float target, float fadeTime)
        {
            float from = buildModeMusic.Volume;
            var t = 0f;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                buildModeMusic.SetBaseVolume(Mathf.Lerp(from, target, t / fadeTime));
                yield return null;
            }
            buildModeMusic.SetBaseVolume(target);
        }
    }
}