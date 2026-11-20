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
        }

        // Rigidbody 대신 직접 적분해야 도착 시점을 던질 때 계산한 비행 시간과 정확히 맞출 수 있다
        private void Update()
        {
            _velocity += Physics.gravity * Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;

            if (transform.position.y <= _landingHeight)
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

            Exploded?.Invoke(transform.position, _explosionRadius);
            Destroy(gameObject);
        }
    }
}
