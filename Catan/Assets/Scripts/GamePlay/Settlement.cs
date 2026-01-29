using System;
using System.Collections.Generic;
using System.Linq;
using GamePlay;
using Misc;
using UI;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using User;

public class Settlement : NetworkBehaviour
{
    public static readonly List<Settlement> AllSettlements = new();
    public static event Action<Settlement> OnSettlementBuild;
    public bool IsOccupied => _level.Value > 0;
    public ulong Owner => _owner.Value;
    public bool Preview { get; set; }
    public int Id => AllSettlements.IndexOf(this);
    public byte Level => _level.Value;

    public Street[] streets;
    private readonly NetworkVariable<ulong> _owner = new(ulong.MaxValue);
    private readonly NetworkVariable<byte> _level = new();

    [SerializeField] private GameObject settlement;
    [SerializeField] private GameObject settlementPreview;
    [SerializeField] private GameObject city;
    [SerializeField] private GameObject cityPreview;
    [SerializeField] private Color canBuildColor;
    [SerializeField] private Color unavailableColor;
    [SerializeField] private AudioSource placeBuildingSound;
    [SerializeField] private ModelColorManager modelColorManager;
    [SerializeField] private float maxPreviewAlpha = 0.5f;
    [SerializeField] private Color defaultColor, selectedColor;
    [SerializeField] private float previewFadeDistance;
    [SerializeField] private Harbor harbor;

    private Material[] _settlementPreviewMaterials;
    private MapTile[] _neighboringTiles;
    private MapIcon _mapIcon;
    private MapIcon _buildPreviewIcon;

    private void OnEnable()
    {
        AllSettlements.Add(this);
    }

    private void OnDisable()
    {
        AllSettlements.Remove(this);
    }

    private void Awake()
    {
        _settlementPreviewMaterials = settlementPreview.GetComponent<Renderer>().materials;
    }

    private void Start()
    {
        _level.OnValueChanged += LevelUpdated;
        LevelUpdated(0, 0);
        _buildPreviewIcon = MapIconManager.AddIcon(transform, IconType.BuildPreview, Color.white);
    }

    private void Update()
    {
        settlementPreview.SetActive(ShowSettlementPreview());
        UpdateCityPreview();
        UpdateBuildPreviewIcon();
    }

    public bool HasHarbor()
    {
        return harbor;
    }

    public Harbor GetHarbor()
    {
        return harbor;
    }

    private void UpdateBuildPreviewIcon()
    {
        if (!IsBuildPreviewIconVisible())
        {
            _buildPreviewIcon.Alpha = 0f;
            return;
        }

        var mousePos = CameraController.Instance.MouseWorldPosition();
        float distanceToMouse = Vector3.Distance(transform.position, mousePos);
        _buildPreviewIcon.Alpha = Mathf.InverseLerp(previewFadeDistance, 0f, distanceToMouse) * maxPreviewAlpha;
        _buildPreviewIcon.SetColor(GetClosestSettlementTo(mousePos) == this ? selectedColor : defaultColor);
    }
    
    private bool IsBuildPreviewIconVisible()
    {
        if (!BuildManager.BuildModeActive) return false;
        if (!CanBeBuildBy(NetworkManager.LocalClientId)) return false;
        if (BuildManager.ActiveBuildType != BuildManager.BuildType.Settlement) return false;
        if (GameManager.Instance.State == GameManager.GameState.Playing &&
            !Player.LocalPlayer.HasResources(BuildManager.GetCostsForBuilding(BuildManager.BuildType.Settlement))) 
            return false;
        return true;
    }

    public static Settlement GetClosestSettlementTo(Vector3 position)
    {
        var settlement = AllSettlements.OrderBy(street => (street.transform.position - position).sqrMagnitude).First();
        if (Vector3.Distance(settlement.transform.position, position) > BuildManager.MaxCursorDistanceFromBuilding)
            return null;
        return settlement;
    }

    public void Build(ulong builderId)
    {
        _owner.Value = builderId;
        _level.Value = 1; // in "LevelUpdated" model gets activated/deactivated based on level
        NotifyConnectedStreets();
    }

    public void Upgrade()
    {
        _level.Value = 2;
    }

    private void NotifyConnectedStreets()
    {
        // Notify all connected streets that a settlement has been built
        foreach (var street in streets)
        {
            if (street && street.IsOccupied)
            {
                street.NotifySettlementBuilt(this);
            }
        }
    }

    private bool HasConnectedRoad(ulong clientId)
    {
        return streets.Any(street => street.Owner == clientId);
    }

    private bool IsBlocked()
    {
        return IsOccupied || streets.Any(street => street.settlements.Any(other => other.IsOccupied));
    }

    public bool CanBeBuildBy(ulong clientId)
    {
        if (IsBlocked()) return false;
        return GameManager.Instance.State == GameManager.GameState.Preparing || HasConnectedRoad(clientId);
    }

    public MapTile[] FindNeighboringTiles()
    {
        if (_neighboringTiles != null)
            return _neighboringTiles;

        var tiles = FindObjectsByType<MapTile>(FindObjectsSortMode.None);
        var orderedTiles = tiles.OrderBy(tile => (tile.transform.position - transform.position).sqrMagnitude).ToArray();
        float closest = (orderedTiles[0].transform.position - transform.position).sqrMagnitude;
        return tiles.Where(tile => (tile.transform.position - transform.position).sqrMagnitude <= (closest + 0.1f))
            .ToArray();
    }

    private bool ShowSettlementPreview()
    {
        if (!Preview || !NetworkManager.Singleton) return false;
        if (IsOccupied) return false;
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        foreach (var material in _settlementPreviewMaterials)
        {
            material.color = CanBeBuildBy(clientId) ? canBuildColor : unavailableColor;   
        }
        Preview = false;
        return true;
    }

    private void UpdateCityPreview()
    {
        if (ShowCityPreview())
        {
            settlement.SetActive(false);
            cityPreview.SetActive(true);
        }
        else
        {
            cityPreview.SetActive(false);
            settlement.SetActive(Level == 1);
        }

        Preview = false;
    }

    private bool ShowCityPreview()
    {
        if (!Preview || !NetworkManager.Singleton) return false;
        if (!IsOccupied) return false;
        if (Owner != NetworkManager.Singleton.LocalClientId) return false;
        return Level == 1;
    }

    private void LevelUpdated(byte previousLevel, byte newLevel)
    {
        settlement.SetActive(newLevel == 1);
        city.SetActive(newLevel == 2);
        if (IsOccupied)
            modelColorManager.SetColor(GameManager.Instance.GetPlayerColor(Owner));
        
        if (newLevel == 1)
        {
            placeBuildingSound.Play();
            _mapIcon = MapIconManager.AddIcon(transform, IconType.Settlement,
                GameManager.Instance.GetPlayerColor(Owner));
            // Notify streets when settlement is first built (level changes from 0 to 1)
            if (previousLevel == 0)
            {
                NotifyConnectedStreets();
            }
            OnSettlementBuild?.Invoke(this);
        }
        else if (newLevel == 2)
        {
            MapIconManager.UpdateIcon(_mapIcon, IconType.City);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Settlement))]
[CanEditMultipleObjects]
public class SettlementEditor : Editor
{
    private const float SmallOffset = 0.01f;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Connect streets"))
        {
            for (var i = 0; i < Selection.gameObjects.Length; i++)
            {
                var targetObject = Selection.gameObjects[i];
                if (!targetObject.TryGetComponent(out Settlement settlement)) continue;
                var streets = FindObjectsByType<Street>(FindObjectsSortMode.None).ToList();
                streets.Sort((s1, s2) => Vector3.Distance(s1.transform.position, targetObject.transform.position)
                    .CompareTo(Vector3.Distance(s2.transform.position, targetObject.transform.position)));
                float closest = Vector3.Distance(streets[0].transform.position, targetObject.transform.position) +
                                SmallOffset;

                var serializedSettlement = new SerializedObject(settlement);
                var streetsProperty = serializedSettlement.FindProperty(nameof(settlement.streets));
                streetsProperty.ClearArray();
                serializedSettlement.ApplyModifiedProperties();
                settlement.streets = streets.Where(street =>
                    Vector3.Distance(street.transform.position, targetObject.transform.position) < closest).ToArray();
            }

            serializedObject.ApplyModifiedProperties();
        }

        if (GUILayout.Button("Place Prefab"))
        {
            foreach (var targetObject in Selection.gameObjects)
            {
                var prefab = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Corner.prefab")) as GameObject;
                if (!prefab) return;
                prefab.transform.position = targetObject.transform.position;
                prefab.transform.rotation = targetObject.transform.rotation;
                prefab.transform.SetParent(targetObject.transform.parent, true);
                prefab.transform.SetSiblingIndex(targetObject.transform.GetSiblingIndex());
                prefab.name = targetObject.name;
            }
        }
    }
}
#endif
