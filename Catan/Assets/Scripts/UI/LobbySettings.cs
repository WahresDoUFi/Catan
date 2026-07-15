using System.Collections;
using GamePlay;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using User;

public class LobbySettings : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private SettingsIntSlider victoryPointSlider;
    [SerializeField]
    private SettingsIntSlider maxCardsOnBanditSlider;
    [SerializeField]
    private SettingsCheckbox revealTilesOnStartCheckbox;
    [SerializeField]
    private SettingsDropdown mapLayoutDropdown;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => GameManager.Instance != null);
        GameManager.Instance.victoryPointsTarget.OnValueChanged += victoryPointSlider.SetValue;
        GameManager.Instance.maxCardsOnBandit.OnValueChanged += maxCardsOnBanditSlider.SetValue;
        GameManager.Instance.revealTilesOnStart.OnValueChanged += revealTilesOnStartCheckbox.SetValue;
        GameManager.Instance.mapLayoutType.OnValueChanged += mapLayoutDropdown.SetValue;

        if (!NetworkManager.Singleton.IsHost)
        {
            victoryPointSlider.SetEnabled(false);
            maxCardsOnBanditSlider.SetEnabled(false);
            revealTilesOnStartCheckbox.SetEnabled(false);
            mapLayoutDropdown.SetEnabled(false);
            yield break;
        }

        GameManager.Instance.victoryPointsTarget.Value = victoryPointSlider.Value;
        victoryPointSlider.ValueChanged += (points) => GameManager.Instance.victoryPointsTarget.Value = points;

        GameManager.Instance.maxCardsOnBandit.Value = maxCardsOnBanditSlider.Value;
        maxCardsOnBanditSlider.ValueChanged += (value) => GameManager.Instance.maxCardsOnBandit.Value = value;

        GameManager.Instance.revealTilesOnStart.Value = revealTilesOnStartCheckbox.Value;
        revealTilesOnStartCheckbox.ValueChanged += (value) => GameManager.Instance.revealTilesOnStart.Value = value;

        GameManager.Instance.mapLayoutType.Value = mapLayoutDropdown.Value;
        mapLayoutDropdown.ValueChanged += (value) => GameManager.Instance.mapLayoutType.Value = value;
    }

    private void Update()
    {
        gameObject.SetActive(GameManager.Instance.State == GameManager.GameState.Waiting);
    }

    void OnDisable()
    {
        CameraController.Instance.Locked = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        CameraController.Instance.Locked = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CameraController.Instance.Locked = false;
    }
}
