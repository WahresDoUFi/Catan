using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BuildManager : MonoBehaviour
{
    public const float MaxCursorDistanceFromBuilding = 10f;
    public enum BuildType
    {
        Street,
        Settlement,
        City
    }
    private static BuildManager _instance;
    public static bool BuildModeActive => _instance._buildModeActive;

    [SerializeField] private Button cancelButton;
    
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
            CameraController.Instance.EnterOverview();
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
        if (GameManager.Instance.PlaceSettlement(Settlement.GetClosestSettlementTo(MouseWorldPosition())))
            SetActive(false);
    }
    private void PlaceStreet()
    {
        if (GameManager.Instance.PlaceStreet(Street.GetClosestStreetTo(MouseWorldPosition())))
            SetActive(false);
    }

    private void HandleBuildingPreview()
    {
        var worldPoint = MouseWorldPosition();
        if (_buildType == BuildType.Street)
            HandleStreetPlacing(worldPoint);
        else if (_buildType == BuildType.Settlement)
            HandleSettlementPlacing(worldPoint);
    }

    private Vector3 MouseWorldPosition()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = _mainCam.transform.position.y;
        return _mainCam.ScreenToWorldPoint(mousePos);
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
}
