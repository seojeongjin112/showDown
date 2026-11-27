using System;
using ArenaBreak.Core;
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
        // 비어 있으면 조준 단계 없이 곧바로 사격 동작에 들어간다
        [SerializeField] private AimLaser _aimLaser;
        [SerializeField] private float _aimDuration = 0.8f;
        // 사격 애니메이션이 시작된 뒤 총구 화염 프레임까지 걸리는 시간이라 이때 맞춰 총알을 낸다.
        // 프레임 번호 / 애니메이션 초당 프레임 수(Outlaw·Gunslinger 4/12, Sniper 5/12)
        [SerializeField] private float _fireDelay = 0.33f;

        private float _nextAttackTime;
        private float _attackStartTime;
        private bool _isAttacking;
        private bool _animationStarted;
        private Transform _attackTarget;

        public float Range => _range;
        public bool IsReady => Time.time >= _nextAttackTime;

        // 사격 애니메이션 시작 시점에 나간다. 총알은 _fireDelay 뒤 화염 프레임에 맞춰 나간다
        public event Action<Transform> Attacked;

        // Attacked는 쏘는 동작을 시작할 때라 실제 발사보다 이르다. 총소리처럼 발사 순간에 맞춰야 하는 연출용이다
        public event Action Fired;

        private float AimTime => _aimLaser == null ? 0f : _aimDuration;

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
            CancelAttack();
        }

        public void Execute(Transform target)
        {
            // 진행 중에는 쿨다운이 이미 걸려 있어 다시 호출되지 않는다
            _nextAttackTime = Time.time + AimTime + _fireDelay + _cooldown;

            _isAttacking = true;
            _animationStarted = false;
            _attackTarget = target;
            _attackStartTime = Time.time;

            // 대기 시간이 없으면 다음 프레임을 기다리지 않고 바로 진행한다
            Advance();
        }

        private void Update()
        {
            Advance();
        }

        private void Advance()
        {
            if (!_isAttacking)
            {
                return;
            }

            // 조준 도중 타겟이 사라지면 쏠 대상이 없다
            if (_attackTarget == null)
            {
                CancelAttack();
                return;
            }

            float elapsed = Time.time - _attackStartTime;

            if (!_animationStarted && elapsed >= AimTime)
            {
                _animationStarted = true;
                Attacked?.Invoke(_attackTarget);
            }

            if (elapsed >= AimTime + _fireDelay)
            {
                Transform target = _attackTarget;
                CancelAttack();
                Fire(target);
                return;
            }

            if (_aimLaser != null)
            {
                float progress = AimTime > 0f ? Mathf.Clamp01(elapsed / AimTime) : 1f;
                _aimLaser.Show(GetMuzzle(), GetAimPoint(_attackTarget), progress);
            }
        }

        private void CancelAttack()
        {
            _isAttacking = false;
            _attackTarget = null;
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

            // 적이 쏠 때도 총구 주변이 잠깐 붉게 밝아진다
            ArenaLighting.Flash(muzzle, new Color(1f, 0.45f, 0.2f), 1.4f, 6f, 0.12f);

            EnemyProjectile projectile = Instantiate(_projectilePrefab, muzzle, Quaternion.identity);
            projectile.Launch(aimPoint - muzzle, target, _aimHeight, _damage);
            Fired?.Invoke();
        }
    }
}
