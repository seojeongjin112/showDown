using System;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 투척마다 생성·파괴가 반복되므로 풀링 대상
    public class EnemyBomb : MonoBehaviour
    {
        [SerializeField] private float _explosionRadius = 3f;

        private Vector3 _velocity;
        private float _landingHeight;

        // Health가 아직 없어서 피해는 주지 않고, 구독자(피해 처리·이펙트)가 이어받게 한다
        public event Action<Vector3, float> Exploded;

        public void Launch(Vector3 velocity, float landingHeight)
        {
            _velocity = velocity;
            _landingHeight = landingHeight;
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
            Exploded?.Invoke(transform.position, _explosionRadius);
            Destroy(gameObject);
        }
    }
}
