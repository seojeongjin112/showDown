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

        private float _nextAttackTime;
        private Transform _cachedTarget;
        private IDamageable _cachedDamageable;

        public float Range => _range;
        public bool IsReady => Time.time >= _nextAttackTime;

        // 애니메이션 같은 연출이 피해 판정과 분리해서 구독할 수 있게 남겨 둔다
        public event Action<Transform> Attacked;

        public void Execute(Transform target)
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
    }
}
