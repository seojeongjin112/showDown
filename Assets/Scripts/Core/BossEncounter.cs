using System;
using System.Collections;
using UnityEngine;

namespace ArenaBreak.Core
{
    // 보스의 체력은 루트 Health 하나이고, 단계 프리팹의 Health는 그 값을 비추는 거울이다.
    // 단계 프리팹이 맞은 피해를 루트로 넘기고, 루트 체력이 단계 끝 비율에 닿으면 그 단계를 쓰러뜨려 다음 단계로 넘긴다.
    // 스포너는 루트 Health.Died로 생존 수를 세므로 단계 사이에 웨이브가 일찍 끝나는 일도 없다
    [RequireComponent(typeof(Health))]
    public class BossEncounter : MonoBehaviour
    {
        [Serializable]
        public class Phase
        {
            [SerializeField] private GameObject _prefab;
            // 앞 단계의 사망 연출이 끝난 뒤에 이 단계를 내보내려고 둔 대기 시간
            [SerializeField] private float _spawnDelay;
            [Tooltip("루트 체력이 전체의 이 비율 이하가 되면 이 단계가 끝난다. 마지막 단계는 체력이 0이 되면 끝나므로 쓰지 않는다")]
            [Range(0f, 1f)]
            [SerializeField] private float _endsAtHealthRatio;

            public GameObject Prefab => _prefab;
            public float SpawnDelay => _spawnDelay;
            public float EndsAtHealthRatio => _endsAtHealthRatio;
        }

        [SerializeField] private Phase[] _phases;

        private Health _encounterHealth;
        private Health _currentHealth;
        private int _phaseIndex = -1;
        // 단계 Health가 줄어든 만큼만 루트로 넘기려고 직전 값을 기억한다
        private int _phaseLast;
        // 거울을 맞추는 피해나 단계를 쓰러뜨리는 피해는 루트로 넘기면 이중으로 깎이므로 끈다
        private bool _forwarding;
        // 피해를 처리하는 도중에 같은 Health를 다시 죽이면 Died가 두 번 나가서 다음 프레임으로 미룬다
        private bool _pendingKill;

        public int PhaseCount => _phases != null ? _phases.Length : 0;

        // 체력바가 단계 이름을 보여 줄 수 있게 0부터 센 현재 단계를 연다
        public int CurrentPhase => _phaseIndex;
        public Health CurrentHealth => _currentHealth;

        public event Action<int, Health> PhaseStarted;

        private void Awake()
        {
            _encounterHealth = GetComponent<Health>();
        }

        private void OnEnable()
        {
            _encounterHealth.Died += OnBossDied;
        }

        private void OnDisable()
        {
            _encounterHealth.Died -= OnBossDied;
        }

        private void Start()
        {
            if (!HasValidSetup())
            {
                // 보스가 영영 안 끝나면 웨이브가 멈추므로 설정이 틀렸을 때는 바로 끝난 것으로 친다
                Finish();
                return;
            }

            SpawnPhase(0, transform.position, transform.rotation);
        }

        private void LateUpdate()
        {
            if (_pendingKill)
            {
                _pendingKill = false;

                if (_currentHealth != null && !_currentHealth.IsDead)
                {
                    _forwarding = false;
                    _currentHealth.TakeDamage(_currentHealth.Max);
                }
            }

            // 미니맵 같은 표시가 루트 Health의 위치를 보므로 현재 단계를 따라다니게 한다
            if (_currentHealth != null)
            {
                transform.position = _currentHealth.transform.position;
            }
        }

        private void OnDestroy()
        {
            if (_currentHealth != null)
            {
                _currentHealth.Died -= OnPhaseDied;
                _currentHealth.Changed -= OnPhaseHealthChanged;
            }
        }

        private bool HasValidSetup()
        {
            if (_phases == null || _phases.Length == 0)
            {
                Debug.LogError("BossEncounter: 단계 프리팹이 비어 있다.", this);
                return false;
            }

            for (int i = 0; i < _phases.Length; i++)
            {
                GameObject prefab = _phases[i].Prefab;
                if (prefab == null || !prefab.TryGetComponent(out Health phaseHealth))
                {
                    Debug.LogError($"BossEncounter: {i}번 단계 프리팹이 없거나 Health가 없다.", this);
                    return false;
                }

                // 최대 체력이 다르면 단계 체력바와 루트 체력이 어긋나 보여서 미리 알려 준다
                if (phaseHealth.Max != _encounterHealth.Max)
                {
                    Debug.LogWarning($"BossEncounter: {i}번 단계 프리팹의 최대 체력이 루트와 다르다.", this);
                }
            }

            return true;
        }

        private void SpawnPhase(int index, Vector3 position, Quaternion rotation)
        {
            GameObject instance = Instantiate(_phases[index].Prefab, position, rotation);
            Health health = instance.GetComponent<Health>();

            // 앞 단계에서 깎인 만큼 새 단계 체력도 깎아 루트와 같은 값에서 시작하게 한다
            _forwarding = false;
            int missing = health.Max - _encounterHealth.Current;
            if (missing > 0)
            {
                health.TakeDamage(missing);
            }

            _phaseIndex = index;
            _currentHealth = health;
            _phaseLast = health.Current;
            health.Changed += OnPhaseHealthChanged;
            health.Died += OnPhaseDied;
            _forwarding = true;

            PhaseStarted?.Invoke(index, health);
        }

        private void OnPhaseHealthChanged(int current, int max)
        {
            int delta = _phaseLast - current;
            _phaseLast = current;

            if (!_forwarding || delta <= 0)
            {
                return;
            }

            // 한 방에 여러 단계를 건너뛰지 않도록 단계가 끝나는 체력에서 피해를 멈추고 그 단계를 끝낸다
            bool isLast = _phaseIndex >= PhaseCount - 1;
            if (!isLast)
            {
                int floor = Mathf.CeilToInt(_phases[_phaseIndex].EndsAtHealthRatio * _encounterHealth.Max);
                int allowed = Mathf.Max(0, _encounterHealth.Current - floor);
                if (delta >= allowed)
                {
                    delta = allowed;
                    _pendingKill = true;
                }
            }

            if (delta > 0)
            {
                _encounterHealth.TakeDamage(delta, _currentHealth.LastDamageSource);
            }
        }

        // 마지막 단계는 루트 체력이 0이 되는 순간 함께 쓰러뜨려 사망 연출이 나오게 한다
        private void OnBossDied()
        {
            _pendingKill = true;
        }

        private void OnPhaseDied()
        {
            // 다음 단계가 이전 단계가 쓰러진 자리에서 이어지도록 구독을 끊기 전에 위치를 잡아 둔다
            Vector3 position = _currentHealth.transform.position;
            Quaternion rotation = _currentHealth.transform.rotation;

            _currentHealth.Died -= OnPhaseDied;
            _currentHealth.Changed -= OnPhaseHealthChanged;
            _currentHealth = null;
            _forwarding = false;

            int next = _phaseIndex + 1;
            if (next >= PhaseCount || _encounterHealth.IsDead)
            {
                Finish();
                return;
            }

            StartCoroutine(SpawnAfterDelay(next, position, rotation));
        }

        private IEnumerator SpawnAfterDelay(int index, Vector3 position, Quaternion rotation)
        {
            float delay = _phases[index].SpawnDelay;
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            SpawnPhase(index, position, rotation);
        }

        private void Finish()
        {
            if (_encounterHealth.IsDead)
            {
                return;
            }

            // 스포너가 이 Health의 Died로 보스를 한 마리로 세어 처치를 인정한다
            _encounterHealth.TakeDamage(_encounterHealth.Max);
        }
    }
}
