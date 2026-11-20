using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(KnifeAttackAnimation))]
    public class KnifeDamage : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _range = 2f;
        [SerializeField] private int _damage = 1;

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

        // 적 콜라이더는 트리거라서 트리거도 맞도록 명시한다. 플레이어 몸 안에서 시작한 레이는 자기 몸을 맞히지 않는다
        private void OnSwung()
        {
            Transform view = _camera.transform;
            if (!Physics.Raycast(view.position, view.forward, out RaycastHit hit, _range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                return;
            }

            hit.collider.GetComponentInParent<IDamageable>()?.TakeDamage(_damage);
        }
    }
}
