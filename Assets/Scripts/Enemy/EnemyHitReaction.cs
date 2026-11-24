using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Enemy
{
    // 피격 모션 그림이 생기기 전까지, 맞은 즉시 번쩍이고 뒤로 밀려서 타격이 들어갔다는 걸 느끼게 한다
    public class EnemyHitReaction : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Color _flashColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private float _flashSeconds = 0.1f;
        [SerializeField] private float _knockbackDistance = 0.6f;
        [SerializeField] private float _knockbackSeconds = 0.12f;
        // 연사로 계속 쏘면 매번 밀려서 적이 제자리에 못 오게 되므로 밀림 사이에 간격을 둔다
        [SerializeField] private float _knockbackCooldown = 0.25f;

        private Health _health;
        private NavMeshAgent _agent;
        private Transform _player;
        private Color _baseColor;
        private Vector3 _knockbackDirection;
        private float _flashRemaining;
        private float _knockbackRemaining;
        private float _nextKnockbackTime;
        private int _lastHealth = -1;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _agent = GetComponent<NavMeshAgent>();

            if (_health == null || _renderer == null)
            {
                Debug.LogError("EnemyHitReaction: Health 또는 렌더러 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            _baseColor = _renderer.color;

            GameObject player = GameObject.FindWithTag(PlayerTag);
            _player = player != null ? player.transform : null;
        }

        private void OnEnable()
        {
            _health.Changed += OnHealthChanged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Changed -= OnHealthChanged;
            _health.Died -= OnDied;
        }

        private void Start()
        {
            // Health가 Awake에서 체력을 채우므로 그 뒤인 여기서 기준값을 잡는다
            if (_lastHealth < 0)
            {
                _lastHealth = _health.Current;
            }
        }

        private void Update()
        {
            if (_flashRemaining > 0f)
            {
                _flashRemaining -= Time.deltaTime;
                if (_flashRemaining <= 0f)
                {
                    _renderer.color = _baseColor;
                }
            }

            if (_knockbackRemaining > 0f)
            {
                float step = Time.deltaTime;
                _knockbackRemaining -= step;

                if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                {
                    _agent.Move(_knockbackDirection * (_knockbackDistance / _knockbackSeconds * step));
                }
            }
        }

        private void OnHealthChanged(int current, int max)
        {
            // 회복이나 최대 체력 증가에는 반응하지 않는다
            bool tookDamage = _lastHealth >= 0 && current < _lastHealth;
            _lastHealth = current;

            if (!tookDamage || current <= 0)
            {
                return;
            }

            _renderer.color = _flashColor;
            _flashRemaining = _flashSeconds;

            if (_player != null && Time.time >= _nextKnockbackTime)
            {
                Vector3 away = transform.position - _player.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.0001f)
                {
                    _knockbackDirection = away.normalized;
                    _knockbackRemaining = _knockbackSeconds;
                    _nextKnockbackTime = Time.time + _knockbackCooldown;
                }
            }
        }

        // 죽은 뒤에 빨간 채로 남거나 시체가 밀리지 않도록 정리한다
        private void OnDied()
        {
            _renderer.color = _baseColor;
            _flashRemaining = 0f;
            _knockbackRemaining = 0f;
        }
    }
}
