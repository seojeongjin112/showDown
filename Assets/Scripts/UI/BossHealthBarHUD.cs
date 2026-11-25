using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    // 보스 체력은 루트 Health 하나이므로 막대도 그 값 하나만 채운다. 단계가 나뉘어도 막대는 나누지 않는다
    public class BossHealthBarHUD : MonoBehaviour
    {
        [SerializeField] private WaveSpawner _spawner;
        [Tooltip("구역형 웨이브(WaveArena)가 내는 보스도 받는다. 스테이지마다 하나씩 넣는다")]
        [SerializeField] private WaveArena[] _arenas;
        [Tooltip("길 위의 조우 지점(EncounterZone)이 내는 보스도 받는다")]
        [SerializeField] private EncounterZone[] _encounters;
        [Tooltip("위 조우 지점과 같은 순서의 보스 이름. 비워 두면 아래 기본 이름을 쓴다")]
        [SerializeField] private string[] _encounterBossNames;

        [SerializeField] private GameObject _root;
        [Tooltip("가로 Filled 방식(왼쪽 기준) Image. 남은 체력 비율만큼 찬다")]
        [SerializeField] private Image _fill;
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _phaseLabel;
        [SerializeField] private string _bossName = "무법자 마차";
        [Tooltip("단계 이름. 비워 두면 단계 이름을 표시하지 않는다")]
        [SerializeField] private string[] _phaseNames;

        private System.Action<Health>[] _encounterHandlers;
        private string _pendingName;
        private BossEncounter _encounter;
        private Health _encounterHealth;

        private void OnEnable()
        {
            _root.SetActive(false);

            if (_spawner != null)
            {
                _spawner.EnemySpawned += OnEnemySpawned;
            }

            if (_arenas != null)
            {
                for (int i = 0; i < _arenas.Length; i++)
                {
                    if (_arenas[i] != null)
                    {
                        _arenas[i].EnemySpawned += OnEnemySpawned;
                    }
                }
            }

            if (_encounters != null)
            {
                _encounterHandlers = new System.Action<Health>[_encounters.Length];

                for (int i = 0; i < _encounters.Length; i++)
                {
                    if (_encounters[i] == null)
                    {
                        continue;
                    }

                    // 보스 구역마다 보스 이름이 달라서, 어느 구역에서 왔는지 알고 이름을 정해 둔다
                    string bossName = _encounterBossNames != null && i < _encounterBossNames.Length && !string.IsNullOrEmpty(_encounterBossNames[i])
                        ? _encounterBossNames[i]
                        : _bossName;
                    _encounterHandlers[i] = health =>
                    {
                        _pendingName = bossName;
                        OnEnemySpawned(health);
                    };
                    _encounters[i].EnemySpawned += _encounterHandlers[i];
                }
            }
        }

        private void OnDisable()
        {
            if (_spawner != null)
            {
                _spawner.EnemySpawned -= OnEnemySpawned;
            }

            if (_arenas != null)
            {
                for (int i = 0; i < _arenas.Length; i++)
                {
                    if (_arenas[i] != null)
                    {
                        _arenas[i].EnemySpawned -= OnEnemySpawned;
                    }
                }
            }

            if (_encounters != null && _encounterHandlers != null)
            {
                for (int i = 0; i < _encounters.Length; i++)
                {
                    if (_encounters[i] != null && _encounterHandlers[i] != null)
                    {
                        _encounters[i].EnemySpawned -= _encounterHandlers[i];
                    }
                }
            }

            _encounterHandlers = null;
            Unbind();
        }

        // 스포너는 적마다 루트 Health만 알려 주므로, 보스인지는 같은 오브젝트의 BossEncounter로 가려낸다
        private void OnEnemySpawned(Health health)
        {
            if (!health.TryGetComponent(out BossEncounter encounter))
            {
                return;
            }

            Bind(encounter, health);
        }

        private void Bind(BossEncounter encounter, Health encounterHealth)
        {
            Unbind();

            _encounter = encounter;
            _encounterHealth = encounterHealth;

            if (_nameLabel != null)
            {
                _nameLabel.text = _pendingName ?? _bossName;
                _pendingName = null;
            }

            _encounter.PhaseStarted += OnPhaseStarted;
            _encounterHealth.Changed += Refresh;
            _encounterHealth.Died += OnBossDied;

            _root.SetActive(true);
            Refresh(_encounterHealth.Current, _encounterHealth.Max);
        }

        private void Unbind()
        {
            if (_encounter != null)
            {
                _encounter.PhaseStarted -= OnPhaseStarted;
                _encounter = null;
            }

            if (_encounterHealth != null)
            {
                _encounterHealth.Changed -= Refresh;
                _encounterHealth.Died -= OnBossDied;
                _encounterHealth = null;
            }

            _root.SetActive(false);
        }

        private void OnPhaseStarted(int index, Health health)
        {
            if (_phaseLabel == null)
            {
                return;
            }

            _phaseLabel.text = _phaseNames != null && index < _phaseNames.Length ? _phaseNames[index] : string.Empty;
        }

        private void OnBossDied()
        {
            Unbind();
        }

        private void Refresh(int current, int max)
        {
            _fill.fillAmount = max > 0 ? (float)current / max : 0f;
        }
    }
}
