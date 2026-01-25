using GamePlay;
using UI.DevelopmentCards;
using UnityEngine;
using UnityEngine.UI;
using User;

namespace UI
{
    public class BuyMenu : MonoBehaviour
    {
        [SerializeField] private Vector3 closedOffset;
        [SerializeField] private float animationSpeed;

        [Header("Buy Buttons")]
        [SerializeField] private Button buyStreetButton;
        [SerializeField] private Button buySettlementButton;
        [SerializeField] private Button buyCityButton;
        [SerializeField] private Button buyDevelopmentCardButton;

        private Vector3 _defaultPosition;
        private Vector3 _targetPosition;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _defaultPosition = _rectTransform.anchoredPosition;
            _targetPosition = _defaultPosition + closedOffset;
            _rectTransform.anchoredPosition = _targetPosition;
        }

        private void Start()
        {
            AddButtonCallbacks();
            DevelopmentCardsDisplay.CardRevealed += DevelopmentCardRevealed;
        }

        private void OnDestroy()
        {
            DevelopmentCardsDisplay.CardRevealed -= DevelopmentCardRevealed;
        }

        private void Update()
        {
            _rectTransform.anchoredPosition = Vector3.Lerp(_rectTransform.anchoredPosition, 
                _targetPosition, Time.deltaTime * animationSpeed);
            UpdateTargetPosition();
        }

        private void AddButtonCallbacks()
        {
            buyStreetButton.onClick.AddListener(() => BuildManager.SelectBuildingType(BuildManager.BuildType.Street));
            buySettlementButton.onClick.AddListener(() => BuildManager.SelectBuildingType(BuildManager.BuildType.Settlement));
            buyCityButton.onClick.AddListener(() => BuildManager.SelectBuildingType(BuildManager.BuildType.City));
            buyDevelopmentCardButton.onClick.AddListener(GameManager.Instance.BuyDevelopmentCard);
            Player.LocalPlayer.DevelopmentCardBought += _ => buyDevelopmentCardButton.interactable = false;
        }

        private void DevelopmentCardRevealed()
        {
            buyDevelopmentCardButton.interactable = true;
        }

        private void UpdateTargetPosition()
        {
            if (GameManager.Instance.State == GameManager.GameState.Playing)
            {
                _targetPosition = 
                    GameManager.Instance.IsMyTurn() &&
                    GameManager.Instance.CanThrowDice() == false && 
                    GameManager.Instance.CardLimitActive == false &&
                    GameManager.Instance.RepositionBandit == false
                        ? _defaultPosition : _defaultPosition + closedOffset;
            }
            else
            {
                _targetPosition = _defaultPosition + closedOffset;   
            }
        }
    }
}
