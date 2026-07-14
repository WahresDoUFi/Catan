using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsIntSlider : MonoBehaviour
{
    public event Action<int> ValueChanged;
    public int Value => (int)slider.value;

    [SerializeField]
    private Slider slider;
    [SerializeField]
    private TMP_InputField valueInput;

    private void Start()
    {
        slider.onValueChanged.AddListener(SliderChanged);
        valueInput.onValueChanged.AddListener(InputFieldEdit);
        valueInput.onEndEdit.AddListener(InputFieldValidate);
        valueInput.SetTextWithoutNotify(slider.value.ToString());
    }

    public void SetEnabled(bool enabled)
    {
        slider.interactable = enabled;
        valueInput.interactable = enabled;
    }
    public void SetValue(int previous, int value)
    {
        slider.SetValueWithoutNotify(value);
        valueInput.SetTextWithoutNotify(value.ToString());
    }

    private void SliderChanged(float value)
    {
        valueInput.SetTextWithoutNotify(value.ToString());
        ValueChanged?.Invoke((int)value);
    }

    private void InputFieldEdit(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Contains("-"))
        {
            slider.SetValueWithoutNotify(slider.minValue);
            return;
        }
        var valueAsFloat = float.Parse(value);
        valueAsFloat = Mathf.Clamp(valueAsFloat, slider.minValue, slider.maxValue);
        slider.SetValueWithoutNotify(valueAsFloat);
        valueInput.SetTextWithoutNotify(valueAsFloat.ToString());
        ValueChanged?.Invoke((int)slider.value);
    }

    private void InputFieldValidate(string _)
    {
        valueInput.SetTextWithoutNotify(slider.value.ToString());
    }
}
