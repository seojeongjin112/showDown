using System;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    public class RangedAttack : MonoBehaviour, IEnemyAttack
    {
        [SerializeField] private EnemyProjectile _projectilePrefab;
        [SerializeField] private float _range = 10f;
        [SerializeField] private float _cooldown = 2f;
        [SerializeField] private float _muzzleHeight = 1f;
        // 플레이어 루트는 발밑이라 그대로 조준하면 탄이 바닥을 향한다
        [SerializeField] private float _aimHeight = 1.5f;

        private float _nextAttackTime;

        public float Range => _range;
        public bool IsReady => Time.time >= _nextAttackTime;

        public event Action<Transform> Attacked;

        private void Awake()
        {
            if (_projectilePrefab == null)
            {
                Debug.LogError("RangedAttack: 투사체 프리팹 참조가 비어 있습니다", this);
                enabled = false;
            }
        }

        public void Execute(Transform target)
        {
            _nextAttackTime = Time.time + _cooldown;

            Vector3 muzzle = transform.position + Vector3.up * _muzzleHeight;
            Vector3 aimPoint = target.position + Vector3.up * _aimHeight;

            EnemyProjectile projectile = Instantiate(_projectilePrefab, muzzle, Quaternion.identity);
            projectile.Launch(aimPoint - muzzle);

            Attacked?.Invoke(target);
        }
    }
}
