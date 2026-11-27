using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace ArenaBreak.Core
{
    // 길 위의 조우 지점. 적은 감지 범위가 없어 미리 깔아 두면 처음부터 전부 추격하므로, 플레이어가 들어올 때 내보낸다
    [RequireComponent(typeof(Collider))]
    public class EncounterZone : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [SerializeField] private WaveData _group;
        // 앞뒤로 흩어 두어야 한 줄로 몰리지 않고 양쪽에서 조여 온다
        [SerializeField] private Transform[] _spawnPoints;
        [Tooltip("이 구역의 적이 동시에 살아 있을 수 있는 최대 수. 죽어서 이 값 아래로 내려가야 다음 적이 나온다")]
        [SerializeField] private int _maxAlive = 10;
        [Tooltip("플레이어와 이보다 가까운 스폰 포인트는 쓰지 않는다. 눈앞에서 튀어나오는 것을 막는다")]
        [SerializeField] private float _minSpawnDistance = 20f;
        [Tooltip("스폰 포인트 둘레로 흩뜨리는 반경. 같은 자리에 겹쳐 나오지 않게 한다")]
        [SerializeField] private float _spawnJitter = 2.5f;

        private readonly List<int> _candidates = new List<int>();
        private readonly List<Health> _spawned = new List<Health>();
        private WaitUntil _waitBelowCap;
        private bool _triggered;
        private int _aliveCount;
        private int _lastPointIndex = -1;

        public event Action<Health> EnemySpawned;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Awake()
        {
            _maxAlive = Difficulty.Scale(_maxAlive, Difficulty.MaxAlive);
            _waitBelowCap = new WaitUntil(() => _aliveCount < _maxAlive);
        }

        private void Start()
        {
            if (_group == null)
            {
                Debug.LogError("EncounterZone: 적 구성 데이터가 비어 있다.", this);
                enabled = false;
                return;
            }

            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                Debug.LogError("EncounterZone: 스폰 포인트가 비어 있다.", this);
                enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered || !other.CompareTag(PlayerTag))
            {
                return;
            }

            // 지나간 뒤 되돌아와도 같은 적이 다시 나오지 않도록 한 번만 쓴다
            _triggered = true;
            StartCoroutine(Spawn(other.transform));
        }

        // 웨이브 구역에 들어서면 지나온 길의 적이 따라와 웨이브와 섞이므로, 이 구역이 낸 살아 있는 적을 치우고 더는 내지 않는다
        public void DespawnAll()
        {
            StopAllCoroutines();

            // 아직 밟지 않은 구역이 나중에 켜져서 웨이브 중에 적을 내보내지 않게 막는다
            _triggered = true;

            for (int i = 0; i < _spawned.Count; i++)
            {
                Health health = _spawned[i];

                // 이미 죽어 쓰러지는 중인 적은 사망 연출이 끝나게 둔다
                if (health != null && !health.IsDead)
                {
                    Destroy(health.gameObject);
                }
            }

            _spawned.Clear();
            _aliveCount = 0;
        }

        private IEnumerator Spawn(Transform player)
        {
            var interval = new WaitForSeconds(_group.SpawnInterval);

            for (int i = 0; i < _group.Entries.Count; i++)
            {
                WaveData.SpawnEntry entry = _group.Entries[i];
                if (entry.EnemyPrefab == null)
                {
                    Debug.LogWarning($"EncounterZone: {_group.name}의 {i}번 항목에 적 프리팹이 없다.", _group);
                    continue;
                }

                int count = Difficulty.Scale(entry.Count, Difficulty.EnemyCount);
                for (int n = 0; n < count; n++)
                {
                    // 한꺼번에 몰리지 않도록 살아 있는 수가 상한 아래일 때만 다음 적을 낸다
                    yield return _waitBelowCap;

                    SpawnEnemy(entry.EnemyPrefab, player);
                    yield return interval;
                }
            }
        }

        private void SpawnEnemy(GameObject prefab, Transform player)
        {
            Vector3 position = PickPosition(player);
            GameObject enemy = Instantiate(prefab, position, Quaternion.identity);

            if (!enemy.TryGetComponent(out Health health))
            {
                // Health가 없으면 사망을 셀 수 없어 상한이 영영 풀리지 않는다
                Debug.LogError($"EncounterZone: {prefab.name}에 Health가 없어 카운트하지 않는다.", prefab);
                return;
            }

            _aliveCount++;
            _spawned.Add(health);

            // 적은 한 번만 죽으므로 핸들러가 스스로 구독을 해제한다
            Action onDied = null;
            onDied = () =>
            {
                health.Died -= onDied;
                _aliveCount--;
            };
            health.Died += onDied;

            EnemySpawned?.Invoke(health);
        }

        private Vector3 PickPosition(Transform player)
        {
            _candidates.Clear();
            int farthest = 0;
            float farthestDistance = -1f;

            for (int i = 0; i < _spawnPoints.Length; i++)
            {
                float distance = FlatDistance(_spawnPoints[i].position, player.position);
                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthest = i;
                }

                if (distance >= _minSpawnDistance && i != _lastPointIndex)
                {
                    _candidates.Add(i);
                }
            }

            // 가까운 곳뿐이면 가장 먼 곳이라도 써서 적이 나오지 않는 일은 막는다
            int index = _candidates.Count > 0 ? _candidates[Random.Range(0, _candidates.Count)] : farthest;
            _lastPointIndex = index;

            Vector3 basePosition = _spawnPoints[index].position;
            Vector2 offset = Random.insideUnitCircle * _spawnJitter;
            Vector3 jittered = basePosition + new Vector3(offset.x, 0f, offset.y);

            return NavMesh.SamplePosition(jittered, out NavMeshHit hit, _spawnJitter + 1f, NavMesh.AllAreas)
                ? hit.position
                : basePosition;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
