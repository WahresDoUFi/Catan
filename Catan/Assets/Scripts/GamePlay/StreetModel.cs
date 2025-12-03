using Unity.Netcode;
using UnityEngine;

public class StreetModel: NetworkBehaviour
{
    [SerializeField] private GameObject _churchDoor1_Prefab;
    [SerializeField] private GameObject _churchDoor2_Prefab;
    [SerializeField] private GameObject _plaza1_Prefab;
    [SerializeField] private GameObject _plaza2_Prefab;

    private void OnEnable()
    {
        _churchDoor1_Prefab.SetActive(false);
        _plaza1_Prefab.SetActive(false);
        _churchDoor2_Prefab.SetActive(false);
        _plaza2_Prefab.SetActive(false);
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
}