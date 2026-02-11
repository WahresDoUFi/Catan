using Unity.Netcode;
using UnityEngine;

public class StreetModel: MonoBehaviour
{
    [SerializeField] private GameObject _churchDoor1_Prefab;
    [SerializeField] private GameObject _churchDoor2_Prefab;
    [SerializeField] private GameObject _plaza1_Prefab;
    [SerializeField] private GameObject _plaza2_Prefab;
    [SerializeField] private GameObject _shortStreet1_Prefab;
    [SerializeField] private GameObject _shortStreet2_Prefab;
    [SerializeField] private GameObject _longStreet_Prefab;

    private void OnEnable()
    {
        _churchDoor1_Prefab.SetActive(false);
        _plaza1_Prefab.SetActive(false);
        _churchDoor2_Prefab.SetActive(false);
        _plaza2_Prefab.SetActive(false);
        _shortStreet1_Prefab.SetActive(false);
        _shortStreet2_Prefab.SetActive(false);
    }
    
    public void SetChurchDoor1Active(bool active)
    {
        _churchDoor1_Prefab.SetActive(active);
        if(active)
        {
            _plaza1_Prefab.SetActive(false);
        }
    }
    
    public void SetChurchDoor2Active(bool active)
    {
        _churchDoor2_Prefab.SetActive(active);
        if(active)
        {
            _plaza2_Prefab.SetActive(false);
        }
    }
    
    public void SetShortStreet1Active(bool active)
    {
        _shortStreet1_Prefab.SetActive(active);
        if(active)
        {
            _shortStreet2_Prefab.SetActive(false);
            _longStreet_Prefab.SetActive(false);
        }
    }
    
    public void SetShortStreet2Active(bool active)
    {
        _shortStreet2_Prefab.SetActive(active);
        if(active)
        {
            _shortStreet1_Prefab.SetActive(false);
            _longStreet_Prefab.SetActive(false);
        }
    }
    
    public void SetLongStreetActive(bool active)
    { 
        _longStreet_Prefab.SetActive(active);
        if(active)
        {
            _shortStreet1_Prefab.SetActive(false);
            _shortStreet2_Prefab.SetActive(false);
        } 
    }
    
    public void SetPlaza1Active(bool active)
    {
        _plaza1_Prefab.SetActive(active);
        if(active)
        {
            _churchDoor1_Prefab.SetActive(false);
        }
    }
    
    public void SetPlaza2Active(bool active)
    {
        _plaza2_Prefab.SetActive(active);
        if(active)
        {
            _churchDoor2_Prefab.SetActive(false);
        }
    }

    public bool IsPlaza1Active()
    {
        return _plaza1_Prefab != null && _plaza1_Prefab.activeSelf;
    }

    public bool IsPlaza2Active()
    {
        return _plaza2_Prefab != null && _plaza2_Prefab.activeSelf;
    }

    public Vector3 GetSide1Position()
    {
        // Use Door1 position as reference for side 1
        return _churchDoor1_Prefab != null ? _churchDoor1_Prefab.transform.position : transform.position;
    }

    public Vector3 GetSide2Position()
    {
        // Use Door2 position as reference for side 2
        return _churchDoor2_Prefab != null ? _churchDoor2_Prefab.transform.position : transform.position;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        GUIStyle redStyle = new GUIStyle();
        redStyle.normal.textColor = Color.red;
        redStyle.fontSize = 14;

        // Visualize Plaza and Door positions in editor with high contrast colors
        if (_churchDoor1_Prefab != null)
        {
            UnityEditor.Handles.Label(_churchDoor1_Prefab.transform.position + Vector3.up * 0.3f, "Door1", redStyle);
        }

        if (_plaza1_Prefab != null)
        {
            UnityEditor.Handles.Label(_plaza1_Prefab.transform.position + Vector3.up * 0.3f, "Plaza1", redStyle);
        }

        if (_churchDoor2_Prefab != null)
        {
            UnityEditor.Handles.Label(_churchDoor2_Prefab.transform.position + Vector3.up * 0.3f, "Door2", redStyle);
        }

        if (_plaza2_Prefab != null)
        {
            UnityEditor.Handles.Label(_plaza2_Prefab.transform.position + Vector3.up * 0.3f, "Plaza2", redStyle);
        }
    }
#endif
}