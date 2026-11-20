using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 발사마다 생성·파괴가 반복되므로 풀링 대상
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 12f;
        [SerializeField] private float _lifetime = 4f;
        [SerializeField] private float _hitRadius = 0.6f;

        private Vector3 _direction;
        private Transform _target;
        private float _targetHeight;
        private IDamageable _damageable;
        private int _damage;

        public void Launch(Vector3 direction, Transform target, float targetHeight, int damage)
        {
            _direction = direction.normalized;
            _target = target;
            _targetHeight = targetHeight;
            _damage = damage;
            // 발사 순간 한 번만 찾으므로 프레임 루프 비용이 아니다
            _damageable = target.GetComponent<IDamageable>();

            Destroy(gameObject, _lifetime);
        }

        private void Update()
        {
            Vector3 previous = transform.position;
            transform.position += _direction * (_speed * Time.deltaTime);

            // 한 프레임에 건물을 뚫고 지나가지 않도록 이동한 구간 전체를 검사한다
            if (ProjectileObstacle.IsBlocked(previous, transform.position))
            {
                Destroy(gameObject);
                return;
            }

            // 플레이어 루트는 발밑이라 조준한 높이 기준으로 맞아야 눈높이로 날아온 탄이 닿는다
            Vector3 hitPoint = _target.position + Vector3.up * _targetHeight;
            if ((hitPoint - transform.position).sqrMagnitude <= _hitRadius * _hitRadius)
            {
                _damageable?.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }
    }
}
