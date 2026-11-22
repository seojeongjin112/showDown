using UnityEngine;

namespace ArenaBreak.Core
{
    public class Coin : Pickup
    {
        [SerializeField] private int _value = 1;

        private CoinWallet _wallet;

        protected override void Awake()
        {
            base.Awake();

            if (Player == null)
            {
                return;
            }

            if (!Player.TryGetComponent(out _wallet))
            {
                Debug.LogError("Coin: Player에 CoinWallet이 없습니다", this);
                enabled = false;
            }
        }

        protected override void OnPickedUp()
        {
            _wallet.Add(_value);
        }
    }
}
