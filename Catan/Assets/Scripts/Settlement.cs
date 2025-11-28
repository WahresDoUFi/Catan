using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class Settlement : NetworkBehaviour
{
    public Street[] streets;
}

#if UNITY_EDITOR
[CustomEditor(typeof(Settlement))]
[CanEditMultipleObjects]
public class SettlementEditor : Editor
{
    private const float SMALL_OFFSET = 0.01f;
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
                                SMALL_OFFSET;

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
