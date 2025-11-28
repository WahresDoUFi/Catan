using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class Street : NetworkBehaviour
{
    public Street[] connectedStreets;
    public Settlement[] settlements;

    public bool HasOwner => _owner.Value >= 0;
    public int Owner => _owner.Value;
    private readonly NetworkVariable<int> _owner = new(-1);

    [SerializeField] private GameObject street;

    public override void OnNetworkSpawn()
    {
        _owner.OnValueChanged += (_, _) => UpdateStreet();
        UpdateStreet();
    }

    public void SetOwner(int ownerId)
    {
        _owner.Value = ownerId;
    }
    
    private void UpdateStreet()
    {
        street.SetActive(HasOwner);
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
                serializedObject.FindProperty(nameof(street.settlements)).ClearArray();
                serializedObject.ApplyModifiedProperties();
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