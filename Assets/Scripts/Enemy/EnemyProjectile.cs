using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 발사마다 생성·파괴가 반복되므로 풀링 대상
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 12f;
        [SerializeField] private float _lifetime = 4f;

        private Vector3 _direction;

        public void Launch(Vector3 direction)
        {
            _direction = direction.normalized;
            Destroy(gameObject, _lifetime);
        }

        private void Update()
        {
            transform.position += _direction * (_speed * Time.deltaTime);
        }
    }
}
