using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ArenaBreak.Core
{
    // 플레이어가 들어오면 입구를 닫고 웨이브를 돌린 뒤, 모두 처치하면 출구를 여는 구역
    [RequireComponent(typeof(Collider))]
    public class WaveArena : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [SerializeField] private WaveData[] _waves;
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private WaveGate _entranceGate;
        [SerializeField] private WaveGate _exitGate;
        [SerializeField] private float _timeBetweenWaves = 3f;
        [Tooltip("웨이브가 시작될 때 이 구역들이 낸 적을 모두 치운다. 지나온 길의 적이 따라 들어와 웨이브와 섞이지 않게 한다")]
        [SerializeField] private EncounterZone[] _clearOnStart;

        private int _aliveCount;
        private bool _started;
        private WaitUntil _waitAllDead;
        private WaitUntil _waitEntranceClosed;

        // 웨이브 번호는 HUD 표시용이라 1부터 센다
        public int CurrentWave { get; private set; }
        public int TotalWaves => _waves != null ? _waves.Length : 0;

        // WaveSpawner와 같은 시그니처를 유지해야 HUD와 미니맵이 소스만 바꿔 끼울 수 있다
        public event Action<int> WaveStarted;
        public event Action<int> WaveCleared;
        public event Action AllWavesCleared;
        public event Action<Health> EnemySpawned;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Awake()
        {
            _waitAllDead = new WaitUntil(() => _aliveCount <= 0);
            _waitEntranceClosed = new WaitUntil(() => !_entranceGate.IsAnimating);
        }

        private void Start()
        {
            if (!HasValidSetup())
            {
                enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_started || !other.CompareTag(PlayerTag))
            {
                return;
            }

            // 문 사이를 오가며 웨이브가 다시 시작되지 않도록 한 번만 돌린다
            _started = true;
            StartCoroutine(RunArena());
        }

        private bool HasValidSetup()
        {
            if (_waves == null || _waves.Length == 0)
            {
                Debug.LogError("WaveArena: 웨이브 데이터가 비어 있다.", this);
                return false;
            }

            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                Debug.LogError("WaveArena: 스폰 포인트가 비어 있다.", this);
                return false;
            }

            return true;
        }

        private IEnumerator RunArena()
        {
            ClearPreviousEnemies();

            if (_entranceGate != null)
            {
                _entranceGate.Close();

                // 문이 닫히는 도중에 웨이브가 시작되면 적이 문 밖으로 나올 수 있어서 닫힌 뒤에 시작한다
                yield return _waitEntranceClosed;
            }

            for (int i = 0; i < _waves.Length; i++)
            {
                yield return new WaitForSeconds(_timeBetweenWaves);

                CurrentWave = i + 1;
                WaveStarted?.Invoke(CurrentWave);

                yield return SpawnWave(_waves[i]);

                // 스폰이 끝나기 전에 앞서 나온 적이 다 죽어도 클리어로 치지 않도록 스폰 완료 뒤에 기다린다
                yield return _waitAllDead;

                WaveCleared?.Invoke(CurrentWave);
            }

            if (_exitGate != null)
            {
                _exitGate.Open();
            }

            AllWavesCleared?.Invoke();
        }

        private void ClearPreviousEnemies()
        {
            if (_clearOnStart == null)
            {
                return;
            }

            for (int i = 0; i < _clearOnStart.Length; i++)
            {
                if (_clearOnStart[i] != null)
                {
                    _clearOnStart[i].DespawnAll();
                }
            }
        }

        private IEnumerator SpawnWave(WaveData wave)
        {
            var interval = new WaitForSeconds(wave.SpawnInterval);

            for (int i = 0; i < wave.Entries.Count; i++)
            {
                WaveData.SpawnEntry entry = wave.Entries[i];
                if (entry.EnemyPrefab == null)
                {
                    Debug.LogWarning($"WaveArena: {wave.name}의 {i}번 항목에 적 프리팹이 없다.", wave);
                    continue;
                }

                for (int n = 0; n < entry.Count; n++)
                {
                    SpawnEnemy(entry.EnemyPrefab);
                    yield return interval;
                }
            }
        }

        private void SpawnEnemy(GameObject prefab)
        {
            Transform point = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
            GameObject enemy = Instantiate(prefab, point.position, point.rotation);

            if (!enemy.TryGetComponent(out Health health))
            {
                // Health가 없으면 사망을 셀 수 없어 웨이브가 영영 안 끝난다
                Debug.LogError($"WaveArena: {prefab.name}에 Health가 없어 카운트하지 않는다.", prefab);
                return;
            }

            _aliveCount++;

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
    }
}
