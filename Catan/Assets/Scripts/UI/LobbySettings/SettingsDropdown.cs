using System;
using TMPro;
using UnityEngine;

public class SettingsDropdown : MonoBehaviour
{
    public event Action<int> ValueChanged;
    public int Value => dropdown.value;

    [SerializeField]
    private TMP_Dropdown dropdown;

    private void Start()
    {
        dropdown.onValueChanged.AddListener(value => ValueChanged?.Invoke(value));
    }

    public void SetEnabled(bool enabled)
    {
        dropdown.interactable = enabled;
    }

    public void SetValue(int previous, int current)
    {
        dropdown.SetValueWithoutNotify(current);
    }
}
