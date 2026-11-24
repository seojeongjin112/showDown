using System;
using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    public class MeleeAttack : MonoBehaviour, IEnemyAttack
    {
        [SerializeField] private float _range = 1.5f;
        [SerializeField] private float _cooldown = 1f;
        [SerializeField] private int _damage = 2;

        [Header("예고")]
        [Tooltip("범위 안에 들어온 뒤 공격 동작을 시작하기까지의 시간(초). 이 동안 느낌표가 뜨고 플레이어가 피할 수 있다")]
        [SerializeField] private float _windupSeconds = 1f;
        [Tooltip("끄면 경고가 뜬 뒤에는 플레이어가 멀리 떨어져도 공격 동작을 끝까지 이어간다")]
        [SerializeField] private bool _cancelWhenFar;
        [Tooltip("예고 중에 범위보다 이만큼 더 멀어지면 헛스윙으로 끝난다(위 항목을 켰을 때만)")]
        [SerializeField] private float _escapeMargin = 1f;
        [Tooltip("피해진 뒤 다시 때릴 수 있을 때까지의 시간. 쫓아가서 바로 다시 예고에 들어간다")]
        [SerializeField] private float _missCooldown = 0.4f;
        [Tooltip("예고 중에 그림이 커지는 비율. 0이면 변화 없음. 곧 때린다는 신호다")]
        [SerializeField] private float _telegraphGrowth = 0.15f;
        [Tooltip("커질 그림 오브젝트. 비어 있으면 자식의 스프라이트를 쓴다")]
        [SerializeField] private Transform _visual;

        [Header("타격")]
        [Tooltip("동작 중에 플레이어가 범위보다 이만큼 더 멀리 있으면 맞지 않는다. 동작이 끝날 때까지 이 안으로 들어오지 않으면 피한 것이다")]
        [SerializeField] private float _hitMargin = 0.6f;

        private float _nextAttackTime;
        private Transform _cachedTarget;
        private IDamageable _cachedDamageable;
        private enum Phase
        {
            None,
            Windup,
            Motion
        }

        private Phase _phase;
        private Transform _target;
        private float _elapsed;
        private bool _damaged;
        private float _motionSeconds = 0.5f;
        private Vector3 _visualBaseScale;

        public float Range => _range;
        public bool IsReady => _phase == Phase.None && Time.time >= _nextAttackTime;

        // 예고가 끝나 공격 동작이 시작되는 순간에 나간다. 동작이 도는 동안 닿는 거리에 있으면 피해가 들어간다
        public event Action<Transform> Attacked;

        // 예고 진행도(0~1)를 매 프레임 알리고, 예고가 끝나면 -1을 보낸다. 머리 위 경고 표시가 구독한다
        public event Action<float> WindupChanged;

        private void Awake()
        {
            if (_visual == null)
            {
                SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
                _visual = sprite != null ? sprite.transform : null;
            }

            if (_visual != null)
            {
                _visualBaseScale = _visual.localScale;
            }

            // 동작 속도가 애니메이터와 다르면 타격 시점이 그림과 어긋나서 같은 값을 쓴다
            EnemySpriteAnimator animator = GetComponent<EnemySpriteAnimator>();
            if (animator != null)
            {
                _motionSeconds = animator.AttackSeconds;
            }
        }

        // 공격이 시작될 수 있다는 신호일 뿐이고, 경고·동작·피해는 아래 Update에서 한 시간표로 이어진다
        public void Execute(Transform target)
        {
            _phase = Phase.Windup;
            _target = target;
            _elapsed = 0f;
            _damaged = false;
        }

        // 느낌표(예고) → 공격 동작. 동작이 도는 처음부터 끝까지 플레이어가 닿는 거리 안에 있으면 한 번 피해를 준다.
        // 느낌표는 동작이 끝날 때까지 남아 있어서 위험한 구간이 보인다
        private void Update()
        {
            if (_phase == Phase.None)
            {
                return;
            }

            _elapsed += Time.deltaTime;

            if (_phase == Phase.Windup)
            {
                // 동작이 시작되기 전에 플레이어가 크게 물러났으면 취소한다. 한번 시작한 동작은 끝까지 간다
                if (_cancelWhenFar && FlatDistanceTo(_target) > _range + _escapeMargin)
                {
                    EndAttack();
                    _nextAttackTime = Time.time + _missCooldown;
                    return;
                }

                float progress = Mathf.Clamp01(_elapsed / Mathf.Max(_windupSeconds, 0.01f));
                ShowTelegraph(progress);
                WindupChanged?.Invoke(progress);

                if (_elapsed >= _windupSeconds)
                {
                    _phase = Phase.Motion;
                    _elapsed = 0f;
                    Attacked?.Invoke(_target);
                }

                return;
            }

            // 동작 중: 닿는 거리에 들어오는 순간 한 번 맞는다
            WindupChanged?.Invoke(1f);

            if (!_damaged && FlatDistanceTo(_target) <= _range + _hitMargin)
            {
                _damaged = true;
                Hurt(_target);
            }

            if (_elapsed >= _motionSeconds)
            {
                EndAttack();
                _nextAttackTime = Time.time + _cooldown;
            }
        }

        // 죽거나 꺼질 때 커진 채로 남지 않도록 되돌린다
        private void OnDisable()
        {
            EndAttack();
        }

        private void Hurt(Transform target)
        {
            // 매번 GetComponent를 하지 않도록 타겟이 바뀔 때만 다시 찾는다
            if (target != _cachedTarget)
            {
                _cachedTarget = target;
                _cachedDamageable = target.GetComponent<IDamageable>();
            }

            _cachedDamageable?.TakeDamage(_damage);
        }

        private void ShowTelegraph(float progress)
        {
            if (_visual != null)
            {
                _visual.localScale = _visualBaseScale * (1f + _telegraphGrowth * progress);
            }
        }

        private void EndAttack()
        {
            _phase = Phase.None;
            _target = null;
            WindupChanged?.Invoke(-1f);

            if (_visual != null)
            {
                _visual.localScale = _visualBaseScale;
            }
        }

        private float FlatDistanceTo(Transform target)
        {
            Vector3 offset = target.position - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }
    }
}
