using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SettingsCheckbox : MonoBehaviour
{
    public event Action<bool> ValueChanged;
    public bool Value => checkbox.isOn;

    [SerializeField]
    private Toggle checkbox;

    private void Start()
    {
        checkbox.onValueChanged.AddListener(value => ValueChanged?.Invoke(value));
    }

    public void SetEnabled(bool enabled)
    {
        checkbox.interactable = enabled;
    }

    public void SetValue(bool previous, bool active)
    {
        checkbox.SetIsOnWithoutNotify(active);
    }
}
