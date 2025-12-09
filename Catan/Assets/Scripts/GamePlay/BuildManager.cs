using System.Collections;
using System;
using System.Linq;
using GamePlay;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using User;

public class BuildManager : MonoBehaviour
{
    public const float MaxCursorDistanceFromBuilding = 10f;

    public enum BuildType
    {
        Street,
        Settlement,
        City
    }

    [Serializable]
    public struct BuildCosts
    {
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

    [SerializeField] private Button cancelButton;
    [SerializeField] private AudioSource buildModeMusic;
    [SerializeField] private BuildCosts[] costs;

    private bool _buildModeActive;
    private BuildType _buildType;
    private Camera _mainCam;

    private void Awake()
    {
        _instance = this;
        cancelButton.onClick.AddListener(() => SetActive(false));
        _mainCam = Camera.main;
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
        _instance._buildModeActive = active;
        if (active)
        {
            CameraController.Instance.EnterOverview();
            _instance.buildModeMusic.volume = 0.4f;
        }
        else
        {
            _instance.StartCoroutine(_instance.LowerVolume());
        }
    }

    public static void SelectBuildingType(BuildType buildType)
    {
        _instance._buildType = buildType;
        SetActive(true);
    }

    public static void ConfirmPosition()
    {
        if (!BuildModeActive) return;
        if (_instance._buildType == BuildType.Street)
            _instance.PlaceStreet();
        else if (_instance._buildType == BuildType.Settlement)
            _instance.PlaceSettlement();
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

    private IEnumerator LowerVolume()
    {
        var startVolume = buildModeMusic.volume;
        const float fadeTime = 3.0f;

        while (buildModeMusic.volume > 0.1f)
        {
            buildModeMusic.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }
    }
}