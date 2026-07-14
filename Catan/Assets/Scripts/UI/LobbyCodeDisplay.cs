using GamePlay;
using Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class LobbyCodeDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _textField;

        [SerializeField]
        private Button copyButton;
        [SerializeField]
        private RectTransform copyConfirmTextPrefab;
        [SerializeField]
        private Vector2 copyTextSpawnOffset;
        [SerializeField]
        private Vector2 copyTextMoveDirection;
        [SerializeField]
        private float copyTextMoveTime;
        [SerializeField]
        private AnimationCurve alphaEaseCurve;
        private CustomEasingFunction<float> _alphaEase;

        private void Awake()
        {
            _textField = GetComponent<TextMeshProUGUI>();
            _alphaEase = new CustomEasingFunction<float>(Mathf.Lerp, alphaEaseCurve);
        }

        private void Start()
        {
            _textField.text = MatchmakingManager.LobbyCode;
            copyButton.onClick.AddListener(CopyButtonPressed);
        }

        public static void CopyCodeToClipboard()
        {
            GUIUtility.systemCopyBuffer = MatchmakingManager.LobbyCode;
        }

        private void CopyButtonPressed()
        {
            CopyCodeToClipboard();
            var copyText = Instantiate(copyConfirmTextPrefab, transform.parent);
            Vector2 spawnPosition = copyTextSpawnOffset;
            UITween.AnimateAlpha(copyText.GetComponent<CanvasGroup>(), 0f, 1f, copyTextMoveTime, _alphaEase.GetValue);
            UITween.AnimatePosition(copyText, spawnPosition, spawnPosition + copyTextMoveDirection, copyTextMoveTime, (start, end, progress) => Vector2.Lerp(start, end, progress));
            UITween.AnimateScale(copyText.GetComponent<MonoBehaviour>(), .8f, 1f, copyTextMoveTime, _alphaEase.GetValue);
            UITween.DelayAction(this, copyTextMoveTime, () => Destroy(copyText.gameObject));
        }
    }
}
