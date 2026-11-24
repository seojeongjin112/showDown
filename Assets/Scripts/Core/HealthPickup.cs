using UnityEngine;

namespace ArenaBreak.Core
{
    // 적을 처치할 때마다 생성·파괴가 반복되므로 풀링 대상
    public class HealthPickup : Pickup
    {
        [SerializeField] private SpriteRenderer _icon;
        [Tooltip("회복량이 1일 때 그림의 긴 변 길이")]
        [SerializeField] private float _smallSize = 0.9f;
        [Tooltip("회복량이 이 값 이상일 때 그림의 긴 변 길이. 많이 주는 것일수록 크게 보인다")]
        [SerializeField] private float _largeSize = 1.6f;
        [SerializeField] private int _largeHeal = 4;

        private Health _health;
        private int _amount;

        protected override void Awake()
        {
            base.Awake();

            if (Player == null)
            {
                return;
            }

            if (!Player.TryGetComponent(out _health) || _icon == null)
            {
                Debug.LogError("HealthPickup: Player에 Health가 없거나 그림 참조가 비어 있습니다", this);
                enabled = false;
            }
        }

        public void Setup(int amount)
        {
            _amount = amount;

            Vector3 spriteSize = _icon.sprite.bounds.size;
            float size = Mathf.Lerp(_smallSize, _largeSize, Mathf.InverseLerp(1f, _largeHeal, amount));
            float scale = size / Mathf.Max(spriteSize.x, spriteSize.y);
            _icon.transform.localScale = new Vector3(scale, scale, 1f);
        }

        protected override void OnPickedUp()
        {
            _health.Heal(_amount);
        }
    }
}
