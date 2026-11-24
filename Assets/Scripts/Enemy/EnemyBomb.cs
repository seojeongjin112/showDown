using System;
using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 투척마다 생성·파괴가 반복되므로 풀링 대상
    public class EnemyBomb : MonoBehaviour
    {
        [SerializeField] private float _explosionRadius = 3f;

        private Vector3 _velocity;
        private float _landingHeight;
        private Transform _target;
        private IDamageable _damageable;
        private int _damage;
        private BombBlastMarker _marker;

        [SerializeField] private Material _markerMaterial;
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField] private float _explosionSeconds = 0.5f;
        [Tooltip("플레이어 폭발 프리팹(반경 4m 기준) 대비 화염 크기 배율")]
        [SerializeField] private float _effectScale = 0.75f;

        // 폭발 이펙트 같은 연출이 피해 판정과 분리해서 구독할 수 있게 남겨 둔다
        public event Action<Vector3, float> Exploded;

        public void Launch(Vector3 velocity, float landingHeight, Transform target, int damage)
        {
            _velocity = velocity;
            _landingHeight = landingHeight;
            _target = target;
            _damage = damage;
            // 투척 순간 한 번만 찾으므로 프레임 루프 비용이 아니다
            _damageable = target.GetComponent<IDamageable>();

            ShowBlastMarker();
        }

        // 착지 높이에 닿는 시각을 중력 포물선에서 풀어 낙하 지점과 남은 시간을 미리 안다
        private void ShowBlastMarker()
        {
            if (_markerMaterial == null)
            {
                return;
            }

            float gravity = Physics.gravity.y;
            float drop = _landingHeight - transform.position.y;
            float discriminant = _velocity.y * _velocity.y + 2f * gravity * drop;
            if (discriminant < 0f || gravity >= 0f)
            {
                return;
            }

            float flightTime = (-_velocity.y - Mathf.Sqrt(discriminant)) / gravity;
            Vector3 landing = transform.position + new Vector3(_velocity.x, 0f, _velocity.z) * flightTime;
            landing.y = _landingHeight;
            _marker = BombBlastMarker.Spawn(_markerMaterial, landing, _explosionRadius, flightTime);
        }

        private void OnDestroy()
        {
            // 중간에 부딪혀 터지면 표시가 허공에 남지 않게 함께 지운다
            if (_marker != null)
            {
                Destroy(_marker.gameObject);
            }
        }

        // Rigidbody 대신 직접 적분해야 도착 시점을 던질 때 계산한 비행 시간과 정확히 맞출 수 있다
        private void Update()
        {
            _velocity += Physics.gravity * Time.deltaTime;
            Vector3 previous = transform.position;
            transform.position += _velocity * Time.deltaTime;

            // 건물 위나 벽을 지나쳐 날아가지 않고 부딪힌 자리에서 터진다
            if (transform.position.y <= _landingHeight || ProjectileObstacle.IsBlocked(previous, transform.position))
            {
                Explode();
            }
        }

        private void Explode()
        {
            // 착지 시점의 위치로 판정해야 던진 뒤에 피한 플레이어는 맞지 않는다
            if (_damageable != null && (_target.position - transform.position).sqrMagnitude <= _explosionRadius * _explosionRadius)
            {
                _damageable.TakeDamage(_damage);
            }

            // 플레이어 다이너마이트와 같은 화염을 쓰되 반경이 다르므로 크기를 맞춘다
            ArenaLighting.Flash(transform.position + Vector3.up * 0.8f, new Color(1f, 0.6f, 0.2f), 3.2f, _explosionRadius * 3f, 0.35f);
            if (_explosionPrefab != null)
            {
                GameObject effect = Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
                effect.transform.localScale *= _effectScale;
                Destroy(effect, _explosionSeconds);
            }

            Exploded?.Invoke(transform.position, _explosionRadius);
            Destroy(gameObject);
        }
    }
}
