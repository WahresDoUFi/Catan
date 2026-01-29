using System.Collections;
using System.Linq;
using GamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using User;

namespace UI
{
    public class PlayerCard : MonoBehaviour, IPointerClickHandler
    {
        public ulong PlayerId => _player ? _player.OwnerClientId : 0;

        [SerializeField] private Image playerColorImage;
        [SerializeField] private Image nameTextImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI cardAmountText;
        [SerializeField] private TextMeshProUGUI victoryPointsText;
        [SerializeField] private TextMeshProUGUI settlementsText;
        [SerializeField] private TextMeshProUGUI streetsText;
        [SerializeField] private float diceRollDelay;
        [SerializeField] private Transform diceOne;
        [SerializeField] private Transform diceTwo;
        [SerializeField] private ResourceCardsTooltip resourceCardsTooltip;
        [Header("Profile Picture")]
        [SerializeField] private Image profileImage;
        [SerializeField] private Sprite[] profileSprites;

        private Player _player;
        private int _resultOne, _resultTwo;
        private bool _rolling;

        private void Start()
        {
            HideDice();
        }

        private void Update()
        {
            if (!_player) return;
            nameText.text = _player.PlayerName;
            profileImage.sprite = profileSprites[_player.PictureId];
            cardAmountText.text = $"{_player.ResourceCount}";
            cardAmountText.color = _player.ResourceCount > GameManager.MaxCardsOnBandit ? Color.red : Color.white;
            victoryPointsText.text = $"{_player.VictoryPoints}";
            settlementsText.text = $"{Settlement.AllSettlements.Count(s => s.Owner == _player.OwnerClientId)}";
            streetsText.text = $"{Street.AllStreets.Count(s => s.Owner == _player.OwnerClientId)}";
            if (_rolling)
            {
                if (GameManager.Instance.DiceThrown)
                {
                    _rolling = false;
                    StopAllCoroutines();
                    SetDiceResult(_resultOne, _resultTwo);
                }
            }
        }

        public void SetPlayer(Player player)
        {
            _player = player;
            resourceCardsTooltip.SetPlayer(player);
            playerColorImage.color = nameTextImage.color = GameManager.Instance.GetPlayerColor(PlayerId);
        }
        
        public void HideDice()
        {
            StopAllCoroutines();
            SetDiceResult(0, 0);
        }

        public void RollDice()
        {
            (_resultOne, _resultTwo) = DiceRoll.GetResult(GameManager.Instance.Seed);
            StartCoroutine(RollDiceCoroutine());
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (GameManager.Instance.CanStealResource && GameManager.Instance.IsMyTurn())
            {
                if (_player.IsLocalPlayer) return;
                GameManager.Instance.StealResource(PlayerId);
            }
        }

        private IEnumerator RollDiceCoroutine()
        {
            _rolling = true;
            while (_rolling)
            {
                yield return new WaitForSeconds(diceRollDelay);
                SetDiceResult(Random.Range(1, 7), Random.Range(1, 7));
            }
        }

        private void SetDiceResult(int first, int second)
        {
            for (var i = 0; i < 6; i++)
            {
                diceOne.GetChild(i).gameObject.SetActive(i + 1 == first);
                diceTwo.GetChild(i).gameObject.SetActive(i + 1 == second);
            }
        }
    }
}
