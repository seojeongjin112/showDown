using System.Collections;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    // 마지막 보스(무법자 마차)를 쓰러뜨리거나 모든 스테이지를 깨면 띄우는 승리 화면. 패배 화면과 같은 틀로 플레이 시간과 처치 수를 보여 준다
    public class VictoryScreen : MonoBehaviour
    {
        [SerializeField] private StageManager _stageManager;
        [Tooltip("마지막 보스를 내보내는 보스 구역. 이 구역의 보스가 쓰러지면 바로 승리한다. 비워 두면 모든 스테이지를 깼을 때만 승리한다")]
        [SerializeField] private EncounterZone _finalBossZone;
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _retryButton;
        // 시점·이동·공격 입력을 꺼야 한다. 시점 스크립트는 커서가 풀려 있으면 클릭으로 다시 잠가 버려서 버튼을 누를 수 없다
        [SerializeField] private Behaviour[] _disableOnVictory;
        [Tooltip("마지막 적이 쓰러지는 모습을 보여 줄 시간")]
        [SerializeField] private float _showDelay = 1.2f;
        [SerializeField] private RunStats _stats;
        // 패널 그림의 줄 사이 칸마다 한 줄씩 들어가도록 시간과 처치 수를 따로 둔다
        [SerializeField] private Text _timeLabel;
        [SerializeField] private Text _killsLabel;
        [Tooltip("{0}은 플레이 시간(분:초)")]
        [SerializeField] private string _timeFormat = "플레이 시간  {0}";
        [Tooltip("{0}은 처치 수")]
        [SerializeField] private string _killsFormat = "처치 수  {0}";

        private Health _finalBoss;
        private bool _shown;

        private void Awake()
        {
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            if (_stageManager != null)
            {
                _stageManager.AllStagesCleared += Show;
            }

            if (_finalBossZone != null)
            {
                _finalBossZone.EnemySpawned += OnFinalBossSpawned;
            }

            _retryButton.onClick.AddListener(Retry);
        }

        private void OnDisable()
        {
            if (_stageManager != null)
            {
                _stageManager.AllStagesCleared -= Show;
            }

            if (_finalBossZone != null)
            {
                _finalBossZone.EnemySpawned -= OnFinalBossSpawned;
            }

            if (_finalBoss != null)
            {
                _finalBoss.Died -= Show;
            }

            _retryButton.onClick.RemoveListener(Retry);
        }

        private void OnFinalBossSpawned(Health boss)
        {
            if (_finalBoss != null)
            {
                return;
            }

            _finalBoss = boss;
            _finalBoss.Died += Show;
        }

        private void Show()
        {
            // 보스 처치와 스테이지 전체 클리어가 겹쳐도 한 번만 띄운다
            if (_shown)
            {
                return;
            }

            _shown = true;

            // 깬 순간의 기록으로 멈춰 둔다
            if (_stats != null)
            {
                _stats.Stop();
            }

            foreach (Behaviour behaviour in _disableOnVictory)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }

            StartCoroutine(ShowAfterDelay());
        }

        private IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSeconds(_showDelay);

            if (_stats != null)
            {
                if (_timeLabel != null)
                {
                    _timeLabel.text = string.Format(_timeFormat, RunStats.FormatTime(_stats.ElapsedSeconds));
                }

                if (_killsLabel != null)
                {
                    _killsLabel.text = string.Format(_killsFormat, _stats.Kills);
                }
            }

            _root.SetActive(true);
            Time.timeScale = 0f;
        }

        private void Retry()
        {
            SceneReloader.Reload(this);
        }
    }
}
