using System;
using UnityEngine;

namespace ArenaBreak.Core
{
    // 웨이브 구역으로 가는 길목의 해골 문. 길 위(조우 구역)의 적을 목표만큼 처치해야 열린다
    public class KillGate : MonoBehaviour
    {
        [SerializeField] private WaveGate _gate;
        [Tooltip("처치 수를 셀 조우 구역. 이 스테이지의 구역을 모두 넣는다")]
        [SerializeField] private EncounterZone[] _zones;
        [SerializeField] private int _requiredKills = 100;
        [Tooltip("문이 잠겨 있는 동안만 보이는 해골 문양")]
        [SerializeField] private GameObject _skullEmblem;

        private int _kills;

        public int Kills => _kills;
        public int RequiredKills => _requiredKills;
        public bool IsUnlocked { get; private set; }

        // HUD가 폴링하지 않고 구독해서 진행도를 갱신한다. 인자는 현재 처치 수와 목표
        public event Action<int, int> ProgressChanged;
        public event Action Unlocked;

        // 이 스테이지의 첫 적이 나온 순간. HUD가 어느 스테이지에 들어왔는지 알고 그 문의 진행도를 보여 준다
        public event Action Engaged;
        private bool _engaged;

        private void Awake()
        {
            if (_gate == null || _zones == null || _zones.Length == 0)
            {
                Debug.LogError("KillGate: 문 또는 조우 구역이 비어 있다.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            for (int i = 0; i < _zones.Length; i++)
            {
                if (_zones[i] != null)
                {
                    _zones[i].EnemySpawned += OnEnemySpawned;
                }
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < _zones.Length; i++)
            {
                if (_zones[i] != null)
                {
                    _zones[i].EnemySpawned -= OnEnemySpawned;
                }
            }
        }

        private void OnEnemySpawned(Health health)
        {
            if (!_engaged)
            {
                _engaged = true;
                Engaged?.Invoke();
            }

            // 적은 한 번만 죽으므로 핸들러가 스스로 구독을 해제한다
            Action onDied = null;
            onDied = () =>
            {
                health.Died -= onDied;
                AddKill();
            };
            health.Died += onDied;
        }

        private void AddKill()
        {
            if (IsUnlocked)
            {
                return;
            }

            _kills++;
            ProgressChanged?.Invoke(_kills, _requiredKills);

            if (_kills >= _requiredKills)
            {
                Unlock();
            }
        }

        private void Unlock()
        {
            IsUnlocked = true;
            _gate.Open();

            if (_skullEmblem != null)
            {
                _skullEmblem.SetActive(false);
            }

            Unlocked?.Invoke();
        }
    }
}
