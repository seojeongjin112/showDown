using UnityEngine;

namespace ArenaBreak.Core
{
    // 게임 진행 이벤트(스테이지·보스·게임오버·클리어)를 듣고 곡을 고르는 일만 한다. 실제 재생과 페이드는 MusicManager가 맡는다
    public class StageMusic : MonoBehaviour
    {
        [SerializeField] private StageManager _stages;
        [Tooltip("보스를 내보내는 조우 지역. 여기서 나온 적이 살아 있는 동안 보스 음악을 튼다")]
        [SerializeField] private EncounterZone[] _bossZones;
        [SerializeField] private Health _playerHealth;
        [Tooltip("비워 두면 게임오버 때 음악을 끈다")]
        [SerializeField] private AudioClip _gameOverMusic;
        [Tooltip("비워 두면 모든 스테이지를 깨도 지금 곡을 계속 튼다")]
        [SerializeField] private AudioClip _victoryMusic;

        private Health _boss;
        // 게임오버 뒤에 보스가 죽거나 스테이지 이벤트가 와도 게임오버 음악을 덮어쓰지 않게 한다
        private bool _gameOver;

        private void Awake()
        {
            if (_stages == null)
            {
                Debug.LogError("StageMusic: 스테이지 관리자가 연결되지 않았습니다", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _stages.StageStarted += OnStageStarted;
            _stages.AllStagesCleared += OnAllStagesCleared;

            if (_bossZones != null)
            {
                foreach (EncounterZone zone in _bossZones)
                {
                    if (zone != null)
                    {
                        zone.EnemySpawned += OnBossSpawned;
                    }
                }
            }

            if (_playerHealth != null)
            {
                _playerHealth.Died += OnPlayerDied;
            }
        }

        private void OnDisable()
        {
            _stages.StageStarted -= OnStageStarted;
            _stages.AllStagesCleared -= OnAllStagesCleared;

            if (_bossZones != null)
            {
                foreach (EncounterZone zone in _bossZones)
                {
                    if (zone != null)
                    {
                        zone.EnemySpawned -= OnBossSpawned;
                    }
                }
            }

            if (_playerHealth != null)
            {
                _playerHealth.Died -= OnPlayerDied;
            }

            ForgetBoss();
        }

        private void OnStageStarted(int stage)
        {
            if (_gameOver)
            {
                return;
            }

            PlayStageMusic();
        }

        private void OnBossSpawned(Health boss)
        {
            if (_gameOver || boss == null)
            {
                return;
            }

            ForgetBoss();
            _boss = boss;
            _boss.Died += OnBossDied;

            StageData data = _stages.CurrentData;
            if (data != null && data.BossMusic != null)
            {
                Play(data.BossMusic);
            }
        }

        private void OnBossDied()
        {
            ForgetBoss();

            if (!_gameOver)
            {
                PlayStageMusic();
            }
        }

        private void OnPlayerDied()
        {
            _gameOver = true;
            Play(_gameOverMusic);
        }

        private void OnAllStagesCleared()
        {
            if (_gameOver || _victoryMusic == null)
            {
                return;
            }

            Play(_victoryMusic);
        }

        private void PlayStageMusic()
        {
            StageData data = _stages.CurrentData;
            if (data != null)
            {
                Play(data.Music);
            }
        }

        private void ForgetBoss()
        {
            if (_boss != null)
            {
                _boss.Died -= OnBossDied;
                _boss = null;
            }
        }

        private static void Play(AudioClip clip)
        {
            MusicManager manager = MusicManager.Instance;
            if (manager == null)
            {
                // 씬에 SceneMusic이 없으면 관리자가 만들어지지 않으므로 조용히 넘기지 않고 알린다
                Debug.LogWarning("StageMusic: 음악 관리자가 없습니다. 씬에 SceneMusic이 있는지 확인하세요");
                return;
            }

            manager.Play(clip);
        }
    }
}
