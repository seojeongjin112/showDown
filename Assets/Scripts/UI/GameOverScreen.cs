using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            foreach (Behaviour behaviour in _disableOnGameOver)
            {
                behaviour.enabled = false;
            }

            _root.SetActive(true);
            Time.timeScale = 0f;
        }

        private void Retry()
        {
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            if (buildIndex < 0)
            {
                Debug.LogError("GameOverScreen: 현재 씬이 Build Profiles의 Scene List에 없어서 다시 시작할 수 없습니다", this);
                return;
            }

            // 멈춘 시간을 되돌리지 않고 로드하면 다음 판도 멈춘 채 시작한다
            Time.timeScale = 1f;
            SceneManager.LoadScene(buildIndex);
        }
    }
}
