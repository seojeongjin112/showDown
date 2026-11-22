using UnityEngine;

namespace ArenaBreak.Core
{
    public class ExpOrb : Pickup
    {
        [SerializeField] private int _value = 1;

        private Experience _experience;

        protected override void Awake()
        {
            base.Awake();

            if (Player == null)
            {
                return;
            }

            if (!Player.TryGetComponent(out _experience))
            {
                Debug.LogError("ExpOrb: Player에 Experience가 없습니다", this);
                enabled = false;
            }
        }

        protected override void OnPickedUp()
        {
            _experience.Add(_value);
        }
    }
}
