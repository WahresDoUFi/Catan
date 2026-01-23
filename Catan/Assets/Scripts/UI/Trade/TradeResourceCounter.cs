using UI.Components;
using User;

namespace UI.Trade
{
    public class TradeResourceCounter : ResourceCounter
    {
        private Player _player;

        public void SetPlayer(Player player)
        {
            _player = player;
            gameObject.SetActive(_player);
            UpdateButtonState();
        }

        protected override void UpdateButtonState()
        {
            base.UpdateButtonState();
            if (_player)
                Limit = _player.GetResources(Resource);
        }
    }
}
