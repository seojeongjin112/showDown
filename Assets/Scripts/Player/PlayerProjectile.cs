using System;
using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    // 활과 토마호크가 쏘는 비행체. 쏠 때마다 생성·파괴가 반복되므로 풀링 대상
    public class PlayerProjectile : MonoBehaviour
    {
        private const string PlayerTag = "Player";
        // 한 구간에서 겹쳐 지나는 콜라이더 수의 상한이라 넉넉히 잡는다
        private const int HitBufferSize = 8;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[HitBufferSize];

        [Tooltip("그림 오브젝트. 토마호크처럼 날면서 도는 것만 돌리고, 몸통은 날아가는 방향을 본다")]
        [SerializeField] private Transform _visual;
        [SerializeField] private float _spinDegreesPerSecond;
        // 허공으로 쏴서 아무것에도 닿지 않아도 영영 날아가지 않도록 상한을 둔다
        [SerializeField] private float _maxLifetime = 4f;

        private Vector3 _velocity;
        private float _gravityScale;
        private int _damage;
        private Action<bool> _onHit;
        private float _age;

        public void Launch(Vector3 velocity, float gravityScale, int damage, Action<bool> onHit)
        {
            _velocity = velocity;
            _gravityScale = gravityScale;
            _damage = damage;
            _onHit = onHit;
            transform.rotation = Quaternion.LookRotation(velocity);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            _velocity += Physics.gravity * (_gravityScale * Time.deltaTime);

            Vector3 step = _velocity * Time.deltaTime;
            float distance = step.magnitude;

            // 한 프레임에 벽이나 적을 뚫고 지나가지 않도록 이동한 구간 전체를 검사한다
            if (distance > Mathf.Epsilon && TryFindHit(step / distance, distance, out RaycastHit hit))
            {
                transform.position = hit.point;
                OnHit(hit.collider);
                return;
            }

            transform.position += step;

            if (_velocity.sqrMagnitude > Mathf.Epsilon)
            {
                transform.rotation = Quaternion.LookRotation(_velocity);
            }

            if (_visual != null && _spinDegreesPerSecond != 0f)
            {
                _visual.Rotate(Vector3.right, _spinDegreesPerSecond * Time.deltaTime, Space.Self);
            }

            if (_age >= _maxLifetime)
            {
                Destroy(gameObject);
            }
        }

        // 쏜 사람의 몸은 건너뛴다. 아래를 보고 쏘면 시작 지점이 자기 콜라이더에 걸리기 때문이다
        private bool TryFindHit(Vector3 direction, float distance, out RaycastHit nearest)
        {
            int count = Physics.RaycastNonAlloc(transform.position, direction, HitBuffer, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            nearest = default;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = HitBuffer[i];
                if (candidate.transform.root.CompareTag(PlayerTag))
                {
                    continue;
                }

                if (!found || candidate.distance < nearest.distance)
                {
                    nearest = candidate;
                    found = true;
                }
            }

            return found;
        }

        private void OnHit(Collider collider)
        {
            IDamageable damageable = collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(_damage);
                _onHit?.Invoke(damageable is Health health && health.IsDead);
            }

            Destroy(gameObject);
        }
    }
}
