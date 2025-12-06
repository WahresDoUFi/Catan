using System;
using System.Collections.Generic;
using System.Linq;
using UI;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class Settlement : NetworkBehaviour
{
    public static readonly List<Settlement> AllSettlements = new();
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
    [SerializeField] private Color canBuildColor;
    [SerializeField] private Color unavailableColor;
    [SerializeField] private AudioClip placeBuildingSound;
    [SerializeField] private ModelColorManager modelColorManager;

    private Material _settlementPreviewMaterial;
    private MapTile[] _neighboringTiles;
    private MapIcon _mapIcon;

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
        _settlementPreviewMaterial = settlementPreview.GetComponent<Renderer>().material;
    }

    private void Start()
    {
        _level.OnValueChanged += LevelUpdated;
        LevelUpdated(0, 0);
    }

    private void Update()
    {
        settlementPreview.SetActive(ShowPreview());
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
        AudioSource.PlayClipAtPoint(placeBuildingSound, Camera.main.transform.position, 0.3f);
        NotifyConnectedStreets();
    }

    private void NotifyConnectedStreets()
    {
        // Notify all connected streets that a settlement has been built
        foreach (var street in streets)
        {
            if (street != null && street.IsOccupied)
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

    private bool ShowPreview()
    {
        if (!Preview || !NetworkManager.Singleton) return false;
        if (IsOccupied) return false;
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        _settlementPreviewMaterial.color = CanBeBuildBy(clientId) ? canBuildColor : unavailableColor;
        Preview = false;
        return true;
    }

    private void LevelUpdated(byte previousLevel, byte newLevel)
    {
        settlement.SetActive(newLevel == 1);
        if (IsOccupied)
            modelColorManager.SetColor(GameManager.Instance.GetPlayerColor(Owner));
        
        if (newLevel == 1)
        {
            _mapIcon = MapIconManager.AddIcon(transform, IconType.Settlement,
                GameManager.Instance.GetPlayerColor(Owner));
            // Notify streets when settlement is first built (level changes from 0 to 1)
            if (previousLevel == 0)
            {
                NotifyConnectedStreets();
            }
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
