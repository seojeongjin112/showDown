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
        [SerializeField] private int _damage = 1;
        // 비어 있거나 조준 시간이 0이면 예전처럼 즉시 발사한다
        [SerializeField] private AimLaser _aimLaser;
        [SerializeField] private float _aimDuration = 0.8f;

        private float _nextAttackTime;
        private float _aimStartTime;
        private bool _isAiming;
        private Transform _aimTarget;

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

        private void OnDisable()
        {
            CancelAim();
        }

        public void Execute(Transform target)
        {
            // 조준 중에는 쿨다운이 이미 걸려 있어 다시 호출되지 않는다
            _nextAttackTime = Time.time + _aimDuration + _cooldown;

            if (_aimLaser == null || _aimDuration <= 0f)
            {
                Fire(target);
                return;
            }

            _isAiming = true;
            _aimTarget = target;
            _aimStartTime = Time.time;
        }

        private void Update()
        {
            if (!_isAiming)
            {
                return;
            }

            float progress = (Time.time - _aimStartTime) / _aimDuration;
            if (progress >= 1f)
            {
                Transform target = _aimTarget;
                CancelAim();
                Fire(target);
                return;
            }

            _aimLaser.Show(GetMuzzle(), GetAimPoint(_aimTarget), progress);
        }

        private void CancelAim()
        {
            _isAiming = false;
            _aimTarget = null;
            if (_aimLaser != null)
            {
                _aimLaser.Hide();
            }
        }

        private Vector3 GetMuzzle() => transform.position + Vector3.up * _muzzleHeight;

        private Vector3 GetAimPoint(Transform target) => target.position + Vector3.up * _aimHeight;

        private void Fire(Transform target)
        {
            Vector3 muzzle = GetMuzzle();
            Vector3 aimPoint = GetAimPoint(target);

            EnemyProjectile projectile = Instantiate(_projectilePrefab, muzzle, Quaternion.identity);
            projectile.Launch(aimPoint - muzzle, target, _aimHeight, _damage);

            Attacked?.Invoke(target);
        }
    }
}
