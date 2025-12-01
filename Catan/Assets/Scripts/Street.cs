using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class Street : NetworkBehaviour
{
    public static readonly List<Street> AllStreets = new();
    
    public Street[] connectedStreets;
    public Settlement[] settlements;
    
    public bool Preview { get; set; }
    public bool IsOccupied => _owner.Value < ulong.MaxValue;
    public ulong Owner => _owner.Value;
    public int Id => AllStreets.IndexOf(this);
    
    private readonly NetworkVariable<ulong> _owner = new(ulong.MaxValue);

    [SerializeField] private GameObject street;
    [SerializeField] private GameObject previewObject;
    [SerializeField] private Color canBuildColor;
    [SerializeField] private Color unavailableColor;

    private Material _previewMaterial;
    
    private void OnEnable()
    {
        AllStreets.Add(this);
    }

    private void OnDisable()
    {
        AllStreets.Remove(this);
    }

    private void Awake()
    {
        _previewMaterial = previewObject.GetComponent<Renderer>().material;
    }

    private void Start()
    {
        _owner.OnValueChanged += (_, _) => UpdateStreet();
        UpdateStreet();
    }

    private void Update()
    {
        previewObject.SetActive(ShowPreview());
    }

    public bool CanBeBuildBy(ulong playerId)
    {
        if (IsOccupied) return false;
        if (GameManager.Instance.State == GameManager.GameState.Preparing)
        {
            //  when placing initial buildings, each settlement needs to have exactly 1 street
            foreach (var settlement in settlements)
            {
                if (settlement.Owner == playerId)
                {
                    if (settlement.streets.Any(otherStreet => otherStreet.IsOccupied))
                        return false;
                }
            }    
        }
        if (connectedStreets.Any(otherStreet => otherStreet.Owner == playerId)) return true;
        return settlements.Any(otherSettlement => otherSettlement.Owner == playerId);
    }

    private bool ShowPreview()
    {
        if (!Preview || !NetworkManager.Singleton) return false;
        _previewMaterial.color = CanBeBuildBy(NetworkManager.Singleton.LocalClientId) ? canBuildColor : unavailableColor;
        Preview = false;
        return true;
    }

    public static Street GetClosestStreetTo(Vector3 position)
    {
        var street = AllStreets.OrderBy(street => (street.transform.position - position).sqrMagnitude).First();
        if (Vector3.Distance(street.transform.position, position) > BuildManager.MaxCursorDistanceFromBuilding)
            return null;
        return street;
    }
    
    public void SetOwner(ulong ownerId)
    {
        _owner.Value = ownerId;
    }
    
    private void UpdateStreet()
    {
        street.SetActive(IsOccupied);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Street)), CanEditMultipleObjects]
public class StreetEditor : Editor
{
    private const float SMALL_OFFSET = 0.01f;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Find Connections"))
        {
            foreach (var targetObject in Selection.gameObjects)
            {
                if (targetObject.TryGetComponent(out Street street) == false)
                    continue;

                var serializedStreet = new SerializedObject(street);
                serializedStreet.FindProperty(nameof(street.connectedStreets)).ClearArray();
                serializedStreet.FindProperty(nameof(street.settlements)).ClearArray();
                serializedStreet.ApplyModifiedProperties();
                var streets = FindObjectsByType<Street>(FindObjectsSortMode.None).ToList();
                streets.Remove(street);
                
                streets.Sort((s1, s2) => Vector3.Distance(s1.transform.position, street.transform.position).CompareTo(Vector3.Distance(s2.transform.position, street.transform.position)));
                float closest = Vector3.Distance(streets[0].transform.position, street.transform.position) + SMALL_OFFSET;
                street.connectedStreets = streets.Where(s => Vector3.Distance(s.transform.position, street.transform.position) <= closest).ToArray();
                
                var settlements = FindObjectsByType<Settlement>(FindObjectsSortMode.None).ToList();
                settlements.Sort((s1, s2) => Vector3.Distance(s1.transform.position, street.transform.position).CompareTo(Vector3.Distance(s2.transform.position, street.transform.position)));
                closest = Vector3.Distance(settlements[0].transform.position, street.transform.position) + SMALL_OFFSET;
                street.settlements = settlements.Where(s => Vector3.Distance(s.transform.position, street.transform.position) <= closest).ToArray();
            }

            serializedObject.ApplyModifiedProperties();
        }

        if (GUILayout.Button("Place Prefab"))
        {
            foreach (var targetObject in Selection.gameObjects)
            {
                var prefab = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Street.prefab")) as GameObject;
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