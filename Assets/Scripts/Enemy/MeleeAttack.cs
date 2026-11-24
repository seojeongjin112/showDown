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
        [Tooltip("범위 안에 들어온 뒤 실제로 때리기까지의 시간(초). 이 동안 플레이어가 물러나서 피할 수 있다")]
        [SerializeField] private float _windupSeconds = 1f;
        [Tooltip("예고 중에 범위보다 이만큼 더 멀어지면 헛스윙으로 끝난다")]
        [SerializeField] private float _escapeMargin = 1f;
        [Tooltip("피해진 뒤 다시 때릴 수 있을 때까지의 시간. 쫓아가서 바로 다시 예고에 들어간다")]
        [SerializeField] private float _missCooldown = 0.4f;
        [Tooltip("예고 중에 그림이 커지는 비율. 0이면 변화 없음. 곧 때린다는 신호다")]
        [SerializeField] private float _telegraphGrowth = 0.15f;
        [Tooltip("커질 그림 오브젝트. 비어 있으면 자식의 스프라이트를 쓴다")]
        [SerializeField] private Transform _visual;

        private float _nextAttackTime;
        private Transform _cachedTarget;
        private IDamageable _cachedDamageable;
        private Transform _windupTarget;
        private float _windupElapsed;
        private bool _isWindingUp;
        private Vector3 _visualBaseScale;

        public float Range => _range;
        public bool IsReady => !_isWindingUp && Time.time >= _nextAttackTime;

        // 애니메이션 같은 연출이 피해 판정과 분리해서 구독할 수 있게 남겨 둔다. 예고가 끝나 실제로 때리는 순간에 나간다
        public event Action<Transform> Attacked;

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
        }

        // 공격이 시작될 수 있다는 신호일 뿐이고, 실제 피해는 예고 시간이 지난 뒤 Update에서 준다
        public void Execute(Transform target)
        {
            if (_windupSeconds <= 0f)
            {
                Strike(target);
                return;
            }

            _isWindingUp = true;
            _windupTarget = target;
            _windupElapsed = 0f;
        }

        private void Update()
        {
            if (!_isWindingUp)
            {
                return;
            }

            // 플레이어가 크게 물러났으면 헛스윙이다. 적은 다시 쫓아와서 처음부터 예고한다
            if (FlatDistanceTo(_windupTarget) > _range + _escapeMargin)
            {
                EndWindup();
                _nextAttackTime = Time.time + _missCooldown;
                return;
            }

            _windupElapsed += Time.deltaTime;
            ShowTelegraph(Mathf.Clamp01(_windupElapsed / _windupSeconds));

            if (_windupElapsed >= _windupSeconds)
            {
                Transform target = _windupTarget;
                EndWindup();
                Strike(target);
            }
        }

        // 죽거나 꺼질 때 커진 채로 남지 않도록 되돌린다
        private void OnDisable()
        {
            EndWindup();
        }

        private void Strike(Transform target)
        {
            _nextAttackTime = Time.time + _cooldown;

            // 매번 GetComponent를 하지 않도록 타겟이 바뀔 때만 다시 찾는다
            if (target != _cachedTarget)
            {
                _cachedTarget = target;
                _cachedDamageable = target.GetComponent<IDamageable>();
            }

            _cachedDamageable?.TakeDamage(_damage);
            Attacked?.Invoke(target);
        }

        private void ShowTelegraph(float progress)
        {
            if (_visual != null)
            {
                _visual.localScale = _visualBaseScale * (1f + _telegraphGrowth * progress);
            }
        }

        private void EndWindup()
        {
            _isWindingUp = false;
            _windupTarget = null;

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
