using System;
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

        // 명중 표시(HUD)가 칼 구현을 몰라도 구독할 수 있게 열어 둔다. 인자는 처치 여부
        public event Action<bool> Hit;

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

        // 트리거 콜라이더도 맞도록 명시한다. 플레이어 몸과 겹친 채 시작한 캐스트는 자기 몸을 맞히지 않는다
        private void OnSwung()
        {
            Transform view = _camera.transform;
            if (!Physics.SphereCast(view.position, _hitRadius, view.forward, out RaycastHit hit, _range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                return;
            }

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                return;
            }

            damageable.TakeDamage(_damage);
            Hit?.Invoke(damageable is Health health && health.IsDead);
        }
    }
}
