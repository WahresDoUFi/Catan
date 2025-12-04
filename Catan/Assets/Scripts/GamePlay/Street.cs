using System.Collections.Generic;
using System.Linq;
using UI;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class Street : NetworkBehaviour
{
    public static readonly List<Street> AllStreets = new();

    public Street[] connectedStreets;
    public Settlement[] settlements;

    [Header("Plaza/Door Side Configuration")]
    [SerializeField] private Settlement settlement_1;  // Maps to Plaza1/Door1
    [SerializeField] private Settlement settlement_2; // Maps to Plaza2/Door2

    public bool Preview { get; set; }
    public bool IsOccupied => _owner.Value < ulong.MaxValue;
    public ulong Owner => _owner.Value;
    public int Id => AllStreets.IndexOf(this);

    private readonly NetworkVariable<ulong> _owner = new(ulong.MaxValue);

    [SerializeField] private GameObject street;
    [SerializeField] private GameObject previewObject;
    [SerializeField] private Color canBuildColor;
    [SerializeField] private Color unavailableColor;
    [SerializeField] private AudioClip placeStreetSound;

    private Material _previewMaterial;
    private StreetModel _streetModel;

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
        _streetModel = street.GetComponentInChildren<StreetModel>();
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
        else if (connectedStreets.Any(otherStreet => otherStreet.Owner == playerId)) return true;

        return settlements.Any(otherSettlement => otherSettlement.Owner == playerId);
    }

    private bool ShowPreview()
    {
        if (!Preview || !NetworkManager.Singleton) return false;
        if (IsOccupied) return false;
        _previewMaterial.color =
            CanBeBuildBy(NetworkManager.Singleton.LocalClientId) ? canBuildColor : unavailableColor;
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
        AudioSource.PlayClipAtPoint(placeStreetSound, Camera.main.transform.position, 0.2f);
    }

    private void UpdateStreet()
    {
        street.SetActive(IsOccupied);
        if (IsOccupied)
        {
            MapIconManager.AddIcon(transform, IconType.Street, GameManager.Instance.GetPlayerColor(Owner));
            UpdateStreetExtras();
        }
    }

    private void UpdateStreetExtras()
    {
        if (_streetModel == null) return;

        // Update left side (Plaza1/Door1)
        if (settlement_1 != null)
        {
            if (settlement_1.IsOccupied)
            {
                // Settlement exists on left side -> activate Door1
                _streetModel.SetChurchDoor1Active(true);
            }
            else if (!IsPlazaActiveOnCorner(settlement_1, true))
            {
                // No settlement and no other Plaza on this corner -> activate Plaza1
                _streetModel.SetPlaza1Active(true);
            }
        }

        // Update right side (Plaza2/Door2)
        if (settlement_2 != null)
        {
            if (settlement_2.IsOccupied)
            {
                // Settlement exists on right side -> activate Door2
                _streetModel.SetChurchDoor2Active(true);
            }
            else if (!IsPlazaActiveOnCorner(settlement_2, false))
            {
                // No settlement and no other Plaza on this corner -> activate Plaza2
                _streetModel.SetPlaza2Active(true);
            }
        }
    }

    private bool IsPlazaActiveOnCorner(Settlement corner, bool isLeft)
    {
        // Check if any other street connected to this corner already has a Plaza active
        foreach (var connectedStreet in corner.streets)
        {
            if (connectedStreet == this || !connectedStreet.IsOccupied) continue;

            var otherStreetModel = connectedStreet.street.GetComponentInChildren<StreetModel>();
            if (otherStreetModel != null)
            {
                // Check which side this corner is on for the other street
                if (connectedStreet.settlement_1 == corner && otherStreetModel.IsPlaza1Active())
                    return true;
                if (connectedStreet.settlement_2 == corner && otherStreetModel.IsPlaza2Active())
                    return true;
            }
        }
        return false;
    }

    public void NotifySettlementBuilt(Settlement settlement)
    {
        if (!IsOccupied || _streetModel == null) return;

        // Determine which side the settlement is on using the configured fields
        if (settlement == settlement_1)
        {
            // Settlement built on left side -> deactivate Plaza1, activate Door1
            _streetModel.SetPlaza1Active(false);
            _streetModel.SetChurchDoor1Active(true);
        }
        else if (settlement == settlement_2)
        {
            // Settlement built on right side -> deactivate Plaza2, activate Door2
            _streetModel.SetPlaza2Active(false);
            _streetModel.SetChurchDoor2Active(true);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Display corner names for all connected settlements
        if (settlements != null)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.red;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;

            foreach (var settlement in settlements)
            {
                if (settlement != null)
                {
                    UnityEditor.Handles.Label(settlement.transform.position + Vector3.up * 0.5f, settlement.name, style);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Create styles for different label types
        GUIStyle redStyle = new GUIStyle();
        redStyle.normal.textColor = Color.red;
        redStyle.fontSize = 14;
        redStyle.fontStyle = FontStyle.Bold;

        GUIStyle blueStyle = new GUIStyle();
        blueStyle.normal.textColor = Color.blue;
        blueStyle.fontSize = 14;
        blueStyle.fontStyle = FontStyle.Bold;

        GUIStyle grayStyle = new GUIStyle();
        grayStyle.normal.textColor = Color.gray;
        grayStyle.fontSize = 14;
        grayStyle.fontStyle = FontStyle.Bold;

        // Show left/right settlement configuration
        if (settlement_1 != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, settlement_1.transform.position);
            Vector3 midPoint = (transform.position + settlement_1.transform.position) * 0.5f;
            UnityEditor.Handles.Label(midPoint, $"LEFT (Plaza1/Door1)\n{settlement_1.name}", redStyle);

            // Also show the corner name at the settlement position
            UnityEditor.Handles.Label(settlement_1.transform.position + Vector3.up * 0.5f, settlement_1.name, redStyle);
        }

        if (settlement_2 != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, settlement_2.transform.position);
            Vector3 midPoint = (transform.position + settlement_2.transform.position) * 0.5f;
            UnityEditor.Handles.Label(midPoint, $"RIGHT (Plaza2/Door2)\n{settlement_2.name}", blueStyle);

            // Also show the corner name at the settlement position
            UnityEditor.Handles.Label(settlement_2.transform.position + Vector3.up * 0.5f, settlement_2.name, redStyle);
        }

        // Also show unassigned settlements for reference
        if (settlements != null)
        {
            foreach (var settlement in settlements)
            {
                if (settlement != null && settlement != settlement_1 && settlement != settlement_2)
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawLine(transform.position, settlement.transform.position);
                    Vector3 midPoint = (transform.position + settlement.transform.position) * 0.5f;
                    UnityEditor.Handles.Label(midPoint, $"UNASSIGNED\n{settlement.name}", grayStyle);

                    // Show corner name
                    UnityEditor.Handles.Label(settlement.transform.position + Vector3.up * 0.5f, settlement.name, redStyle);
                }
            }
        }
    }
#endif
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

                streets.Sort((s1, s2) => Vector3.Distance(s1.transform.position, street.transform.position)
                    .CompareTo(Vector3.Distance(s2.transform.position, street.transform.position)));
                float closest = Vector3.Distance(streets[0].transform.position, street.transform.position) +
                                SMALL_OFFSET;
                street.connectedStreets = streets
                    .Where(s => Vector3.Distance(s.transform.position, street.transform.position) <= closest).ToArray();

                var settlements = FindObjectsByType<Settlement>(FindObjectsSortMode.None).ToList();
                settlements.Sort((s1, s2) =>
                    Vector3.Distance(s1.transform.position, street.transform.position)
                        .CompareTo(Vector3.Distance(s2.transform.position, street.transform.position)));
                closest = Vector3.Distance(settlements[0].transform.position, street.transform.position) + SMALL_OFFSET;
                street.settlements = settlements
                    .Where(s => Vector3.Distance(s.transform.position, street.transform.position) <= closest).ToArray();
            }

            serializedObject.ApplyModifiedProperties();
        }

        if (GUILayout.Button("Place Prefab"))
        {
            foreach (var targetObject in Selection.gameObjects)
            {
                var prefab =
                    PrefabUtility.InstantiatePrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Street.prefab")) as GameObject;
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