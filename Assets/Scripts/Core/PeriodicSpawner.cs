using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace ArenaBreak.Core
{
    // 보스가 나온 뒤부터 보스가 쓰러질 때까지 정해진 자리(건물 앞)에서 졸개를 주기적으로 내보낸다
    public class PeriodicSpawner : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [Tooltip("이 구역이 첫 적(보스)을 내보내는 순간부터 시작하고, 그 보스가 죽으면 멈춘다")]
        [SerializeField] private EncounterZone _bossZone;
        [SerializeField] private GameObject[] _prefabs;
        [Tooltip("건물 문 앞 같은 스폰 자리. 플레이어와 너무 가까운 자리는 건너뛴다")]
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private float _interval = 4f;
        [Tooltip("한 번에 내보내는 수")]
        [SerializeField] private int _perBurst = 1;
        [Tooltip("살아 있는 졸개가 이 수 이상이면 다음 주기까지 내보내지 않는다")]
        [SerializeField] private int _maxAlive = 6;
        [SerializeField] private float _minDistanceFromPlayer = 10f;
        [SerializeField] private float _jitter = 1.5f;

        private Transform _player;
        private Health _boss;
        private Coroutine _routine;
        private int _aliveCount;

        private void Awake()
        {
            GameObject player = GameObject.FindWithTag(PlayerTag);
            if (_bossZone == null || player == null || _prefabs == null || _prefabs.Length == 0 || _spawnPoints == null || _spawnPoints.Length == 0)
            {
                Debug.LogError("PeriodicSpawner: 보스 구역, 플레이어, 프리팹 또는 스폰 자리가 비어 있습니다", this);
                enabled = false;
                return;
            }

            _player = player.transform;
        }

        private void OnEnable()
        {
            _bossZone.EnemySpawned += OnBossSpawned;
        }

        private void OnDisable()
        {
            _bossZone.EnemySpawned -= OnBossSpawned;
            StopSpawning();
        }

        private void OnBossSpawned(Health boss)
        {
            if (_boss != null)
            {
                return;
            }

            _boss = boss;
            _boss.Died += OnBossDied;
            _routine = StartCoroutine(Run());
        }

        private void OnBossDied()
        {
            StopSpawning();
        }

        private void StopSpawning()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (_boss != null)
            {
                _boss.Died -= OnBossDied;
                _boss = null;
            }
        }

        private IEnumerator Run()
        {
            var wait = new WaitForSeconds(_interval);

            while (true)
            {
                yield return wait;

                for (int i = 0; i < _perBurst && _aliveCount < _maxAlive; i++)
                {
                    SpawnOne();
                }
            }
        }

        private void SpawnOne()
        {
            // 눈앞에서 튀어나오지 않도록 플레이어와 먼 자리만 쓴다. 모두 가깝다면 이번 주기는 건너뛴다
            Transform point = PickPoint();
            if (point == null)
            {
                return;
            }

            Vector2 jitter = Random.insideUnitCircle * _jitter;
            Vector3 wanted = point.position + new Vector3(jitter.x, 0f, jitter.y);
            Vector3 position = NavMesh.SamplePosition(wanted, out NavMeshHit hit, 4f, NavMesh.AllAreas) ? hit.position : point.position;

            GameObject enemy = Instantiate(_prefabs[Random.Range(0, _prefabs.Length)], position, point.rotation);
            if (!enemy.TryGetComponent(out Health health))
            {
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
        }

        private Transform PickPoint()
        {
            float minSqr = _minDistanceFromPlayer * _minDistanceFromPlayer;
            int start = Random.Range(0, _spawnPoints.Length);

            for (int i = 0; i < _spawnPoints.Length; i++)
            {
                Transform candidate = _spawnPoints[(start + i) % _spawnPoints.Length];
                Vector3 offset = candidate.position - _player.position;
                offset.y = 0f;
                if (offset.sqrMagnitude >= minSqr)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
