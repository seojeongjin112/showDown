using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 튜토리얼의 맞아 주는 표적. 근접과 원거리로 맞았을 때 서로 다른 모션을 보여 주고, 맞은 만큼 바로 회복해서 쓰러지지 않는다
    [RequireComponent(typeof(Health))]
    public class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite[] _idle;
        [SerializeField] private Sprite[] _hitMelee;
        [SerializeField] private Sprite[] _hitRanged;
        [SerializeField] private float _framesPerSecond = 10f;

        private Health _health;
        private Sprite[] _playing;
        private float _elapsed;
        private int _lastHealth = -1;
        private bool _needsHeal;

        private void Awake()
        {
            _health = GetComponent<Health>();

            if (_renderer == null || _idle == null || _idle.Length == 0)
            {
                Debug.LogError("TrainingDummy: 렌더러 또는 대기 모션이 비어 있습니다", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _health.Changed += OnHealthChanged;
        }

        private void OnDisable()
        {
            _health.Changed -= OnHealthChanged;
        }

        private void Start()
        {
            // Health가 Awake에서 체력을 채우므로 그 뒤인 여기서 기준값을 잡는다
            _lastHealth = _health.Current;
        }

        private void Update()
        {
            // 변화 이벤트 안에서 바로 회복하면 같은 이벤트가 겹쳐 나가서 다음 프레임에 회복한다
            if (_needsHeal)
            {
                _needsHeal = false;
                _health.Heal(_health.Max - _health.Current);
                _lastHealth = _health.Current;
            }

            _elapsed += Time.deltaTime;
            int frame = (int)(_elapsed * _framesPerSecond);

            if (_playing != null)
            {
                if (frame >= _playing.Length)
                {
                    _playing = null;
                    _elapsed = 0f;
                    return;
                }

                _renderer.sprite = _playing[frame];
                return;
            }

            _renderer.sprite = _idle[frame % _idle.Length];
        }

        private void OnHealthChanged(int current, int max)
        {
            bool tookDamage = _lastHealth >= 0 && current < _lastHealth;
            _lastHealth = current;

            if (!tookDamage)
            {
                return;
            }

            Sprite[] frames = _health.LastDamageSource == DamageSource.Melee ? _hitMelee : _hitRanged;
            if (frames != null && frames.Length > 0)
            {
                _playing = frames;
                _elapsed = 0f;
            }

            _needsHeal = true;
        }
    }
}
