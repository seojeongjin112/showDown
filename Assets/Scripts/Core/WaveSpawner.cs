using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ArenaBreak.Core
{
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private WaveData[] _waves;
        [SerializeField] private Transform[] _spawnPoints;

        // 강화 선택 UI가 생기기 전까지 웨이브 사이 숨 돌릴 틈으로 쓰는 임시 대기 시간
        [SerializeField] private float _timeBetweenWaves = 3f;

        private int _aliveCount;
        private WaitUntil _waitAllDead;

        // 웨이브 번호는 HUD 표시용이라 1부터 센다
        public int CurrentWave { get; private set; }
        public int TotalWaves => _waves != null ? _waves.Length : 0;

        public event Action<int> WaveStarted;
        public event Action<int> WaveCleared;
        public event Action AllWavesCleared;

        // 미니맵 같은 표시 쪽이 매 프레임 적을 찾지 않고 생성 시점에 구독해서 추적할 수 있게 한다
        public event Action<Health> EnemySpawned;

        private void Awake()
        {
            _waitAllDead = new WaitUntil(() => _aliveCount <= 0);
        }

        private void Start()
        {
            if (!HasValidSetup())
            {
                enabled = false;
                return;
            }

            StartCoroutine(RunWaves());
        }

        private bool HasValidSetup()
        {
            if (_waves == null || _waves.Length == 0)
            {
                Debug.LogError("WaveSpawner: 웨이브 데이터가 비어 있다.", this);
                return false;
            }

            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                Debug.LogError("WaveSpawner: 스폰 포인트가 비어 있다.", this);
                return false;
            }

            return true;
        }

        private IEnumerator RunWaves()
        {
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

            AllWavesCleared?.Invoke();
        }

        private IEnumerator SpawnWave(WaveData wave)
        {
            var interval = new WaitForSeconds(wave.SpawnInterval);

            for (int i = 0; i < wave.Entries.Count; i++)
            {
                WaveData.SpawnEntry entry = wave.Entries[i];
                if (entry.EnemyPrefab == null)
                {
                    Debug.LogWarning($"WaveSpawner: {wave.name}의 {i}번 항목에 적 프리팹이 없다.", wave);
                    continue;
                }

                int count = Difficulty.Scale(entry.Count, Difficulty.EnemyCount);
                for (int n = 0; n < count; n++)
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
                Debug.LogError($"WaveSpawner: {prefab.name}에 Health가 없어 카운트하지 않는다.", prefab);
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
