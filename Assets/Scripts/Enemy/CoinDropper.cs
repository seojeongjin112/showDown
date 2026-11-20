using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Enemy
{
    [RequireComponent(typeof(Health))]
    public class CoinDropper : MonoBehaviour
    {
        [SerializeField] private Coin _coinPrefab;
        [SerializeField] private int _count = 1;
        [SerializeField] private float _scatterRadius = 1.2f;
        // 가슴 높이에서 튀어나오게 해 발밑에서 바로 솟아 보이지 않게 한다
        [SerializeField] private float _spawnHeight = 1f;
        [SerializeField] private float _snapDistance = 1.5f;

        private Health _health;

        private void Awake()
        {
            _health = GetComponent<Health>();

            if (_coinPrefab == null)
            {
                Debug.LogError("CoinDropper: 코인 프리팹 참조가 비어 있습니다", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
        }

        private void OnDied()
        {
            Vector3 origin = transform.position;

            for (int i = 0; i < _count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * _scatterRadius;
                Vector3 landing = origin + new Vector3(offset.x, 0f, offset.y);

                // 벽 가까이서 죽어도 코인이 건물 안에 떨어져 못 줍는 일이 없도록 걸을 수 있는 바닥으로 옮긴다
                if (NavMesh.SamplePosition(landing, out NavMeshHit hit, _snapDistance, NavMesh.AllAreas))
                {
                    landing = hit.position;
                }

                Coin coin = Instantiate(_coinPrefab, origin + Vector3.up * _spawnHeight, Quaternion.identity);
                coin.Launch(landing);
            }
        }
    }
}
