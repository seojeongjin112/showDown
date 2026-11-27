using System.Collections;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private Health _playerHealth;
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _retryButton;
        // 시점·이동·공격 입력을 꺼야 한다. 시점 스크립트는 커서가 풀려 있으면 클릭으로 다시 잠가 버려서 버튼을 누를 수 없다
        [SerializeField] private Behaviour[] _disableOnGameOver;
        // 플레이어 사망 모션을 보여 줄 시간
        [SerializeField] private float _showDelay = 1.2f;
        [SerializeField] private RunStats _stats;
        // 패널 그림의 줄 사이 칸마다 한 줄씩 들어가도록 시간과 처치 수를 따로 둔다
        [SerializeField] private Text _timeLabel;
        [SerializeField] private Text _killsLabel;
        [Tooltip("{0}은 플레이 시간(분:초)")]
        [SerializeField] private string _timeFormat = "플레이 시간  {0}";
        [Tooltip("{0}은 처치 수")]
        [SerializeField] private string _killsFormat = "처치 수  {0}";

        private void Awake()
        {
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            if (_playerHealth != null)
            {
                _playerHealth.Died += Show;
            }

            _retryButton.onClick.AddListener(Retry);
        }

        private void OnDisable()
        {
            if (_playerHealth != null)
            {
                _playerHealth.Died -= Show;
            }

            _retryButton.onClick.RemoveListener(Retry);
        }

        private void Show()
        {
            // 죽은 순간의 기록으로 멈춰 둔다. 사망 모션을 보는 동안 시간이 더 쌓이지 않게 한다
            if (_stats != null)
            {
                _stats.Stop();
            }

            foreach (Behaviour behaviour in _disableOnGameOver)
            {
                behaviour.enabled = false;
            }

            StartCoroutine(ShowAfterDeathMotion());
        }

        // timeScale을 먼저 0으로 만들면 사망 모션이 재생되지 않으므로, 모션이 끝난 뒤에 화면을 띄우고 멈춘다
        private IEnumerator ShowAfterDeathMotion()
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
