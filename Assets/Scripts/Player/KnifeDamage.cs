using System;
using System.Collections.Generic;
using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(KnifeAttackAnimation))]
    public class KnifeDamage : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _range = 4f;

        // 두께 없는 선은 스프라이트 가장자리를 겨냥해도 더 좁은 콜라이더를 지나쳐서 허공을 벤 것처럼 된다
        [SerializeField] private float _hitRadius = 0.7f;
        [SerializeField] private int _damage = 1;

        public void AddDamage(int amount)
        {
            _damage += amount;
        }

        public void AddRange(float amount)
        {
            _range += amount;
        }

        // 명중 표시(HUD)가 칼 구현을 몰라도 구독할 수 있게 열어 둔다. 인자는 처치 여부
        public event Action<bool> Hit;

        // 칼 반경 안에 겹칠 수 있는 콜라이더 수의 상한
        private const int MaxOverlaps = 8;

        private readonly Collider[] _overlaps = new Collider[MaxOverlaps];
        private readonly RaycastHit[] _castHits = new RaycastHit[MaxOverlaps];
        // 한 번 휘두르는 동안 이미 맞은 대상은 다시 맞지 않는다
        private readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();
        private KnifeAttackAnimation _animation;

        private void Awake()
        {
            _animation = GetComponent<KnifeAttackAnimation>();

            if (_camera == null)
            {
                _camera = GetComponentInParent<Camera>();
            }

            if (_camera == null)
            {
                Debug.LogError("KnifeDamage: 카메라를 찾을 수 없습니다", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _animation.Swung += OnSwung;
        }

        private void OnDisable()
        {
            _animation.Swung -= OnSwung;
        }

        private void OnSwung()
        {
            _hitThisSwing.Clear();
            TryHit();
        }

        // 휘두르는 모션이 도는 처음부터 끝까지 판정을 이어 가서, 중간에 범위로 들어온 적도 맞는다
        private void Update()
        {
            if (_animation.IsPlaying)
            {
                TryHit();
            }
        }

        // 트리거 콜라이더도 맞도록 명시한다. 플레이어 몸과 겹친 채 시작한 캐스트는 자기 몸을 맞히지 않는다
        private void TryHit()
        {
            IDamageable damageable = FindOverlappingTarget() ?? FindCastTarget();
            if (damageable == null)
            {
                return;
            }

            _hitThisSwing.Add(damageable);

            // 근접으로 쓰러뜨린 적만 탄약을 떨어뜨리므로 출처를 함께 알린다
            if (damageable is Health health)
            {
                health.TakeDamage(_damage, DamageSource.Melee);
                Hit?.Invoke(health.IsDead);
                return;
            }

            damageable.TakeDamage(_damage);
            Hit?.Invoke(false);
        }

        // 적이 몸에 바짝 붙으면 스피어 시작 위치부터 겹쳐서 캐스트가 그 적을 건너뛰므로, 겹친 대상을 먼저 찾는다
        private IDamageable FindOverlappingTarget()
        {
            Transform view = _camera.transform;
            int count = Physics.OverlapSphereNonAlloc(view.position, _hitRadius, _overlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

            IDamageable nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider overlap = _overlaps[i];
                if (overlap.transform.IsChildOf(view.root))
                {
                    continue;
                }

                IDamageable candidate = overlap.GetComponentInParent<IDamageable>();
                if (candidate == null || _hitThisSwing.Contains(candidate))
                {
                    continue;
                }

                // 등 뒤에 붙은 적까지 베면 안 되므로 시선 앞쪽에 있는 것만 본다
                Vector3 toTarget = overlap.bounds.center - view.position;
                if (Vector3.Dot(view.forward, toTarget) <= 0f || toTarget.sqrMagnitude >= nearestDistance)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = toTarget.sqrMagnitude;
            }

            return nearest;
        }

        private IDamageable FindCastTarget()
        {
            Transform view = _camera.transform;
            int count = Physics.SphereCastNonAlloc(view.position, _hitRadius, view.forward, _castHits, _range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

            IDamageable nearest = null;
            float nearestDistance = float.MaxValue;
            bool blocked = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _castHits[i];

                // 자기 몸에서 시작한 구체는 자기 몸을 맞힌다
                if (hit.distance >= nearestDistance || hit.collider.transform.IsChildOf(view.root))
                {
                    continue;
                }

                IDamageable candidate = hit.collider.GetComponentInParent<IDamageable>();

                // 이미 맞은 대상은 없는 것처럼 지나쳐 뒤의 적을 벨 수 있게 한다
                if (candidate != null && _hitThisSwing.Contains(candidate))
                {
                    continue;
                }

                // 조우 구역 같은 트리거 영역은 칼을 막지 않는다
                if (candidate == null && hit.collider.isTrigger)
                {
                    continue;
                }

                // 건물 같은 장애물이 먼저 닿으면 그 뒤의 적은 벨 수 없다
                nearestDistance = hit.distance;
                nearest = candidate;
                blocked = candidate == null;
            }

            return blocked ? null : nearest;
        }
    }
}
