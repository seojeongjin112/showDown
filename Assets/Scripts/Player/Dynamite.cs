using System;
using System.Collections.Generic;
using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    // 던질 때마다 생성·파괴가 반복되므로 풀링 대상
    public class Dynamite : MonoBehaviour
    {
        private const string PlayerTag = "Player";
        private const int OverlapBufferSize = 32;

        // 폭발마다 배열을 새로 만들지 않도록 결과 버퍼를 재사용한다
        private static readonly Collider[] Overlaps = new Collider[OverlapBufferSize];

        [SerializeField] private float _blastRadius = 4f;
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField] private float _explosionSeconds = 0.5f;
        // 허공으로 던져 아무것에도 닿지 않아도 영영 날아가지 않도록 상한을 둔다
        [SerializeField] private float _maxLifetime = 5f;

        private readonly HashSet<IDamageable> _damaged = new HashSet<IDamageable>();
        private Vector3 _velocity;
        private int _damage;
        private Action<bool> _onHit;
        private float _age;

        public void Launch(Vector3 velocity, int damage, Action<bool> onHit)
        {
            _velocity = velocity;
            _damage = damage;
            _onHit = onHit;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            _velocity += Physics.gravity * Time.deltaTime;

            Vector3 step = _velocity * Time.deltaTime;
            float distance = step.magnitude;

            // 한 프레임에 벽이나 적을 뚫고 지나가지 않도록 이동한 구간 전체를 검사한다
            if (distance > Mathf.Epsilon
                && Physics.Raycast(transform.position, step / distance, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                transform.position = hit.point;
                Explode();
                return;
            }

            transform.position += step;

            if (_age >= _maxLifetime)
            {
                Explode();
            }
        }

        private void Explode()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _blastRadius, Overlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            bool anyHit = false;
            bool anyKill = false;

            for (int i = 0; i < count; i++)
            {
                // 던진 사람은 폭발에 다치지 않는다
                if (Overlaps[i].transform.root.CompareTag(PlayerTag))
                {
                    continue;
                }

                IDamageable damageable = Overlaps[i].GetComponentInParent<IDamageable>();

                // 한 적에 콜라이더가 여러 개여도 한 번만 맞아야 한다
                if (damageable == null || !_damaged.Add(damageable))
                {
                    continue;
                }

                damageable.TakeDamage(_damage);
                anyHit = true;
                anyKill |= damageable is Health health && health.IsDead;
            }

            // 폭발 순간에 주변이 크게 환해진다
            ArenaLighting.Flash(transform.position + Vector3.up * 0.8f, new Color(1f, 0.6f, 0.2f), 3.2f, _blastRadius * 3f, 0.35f);

            if (_explosionPrefab != null)
            {
                GameObject effect = Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
                Destroy(effect, _explosionSeconds);
            }

            if (anyHit)
            {
                _onHit?.Invoke(anyKill);
            }

            Destroy(gameObject);
        }
    }
}
