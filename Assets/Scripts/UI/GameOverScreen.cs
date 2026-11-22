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

            StartCoroutine(ShowAfterDeathMotion());
        }

        // timeScale을 먼저 0으로 만들면 사망 모션이 재생되지 않으므로, 모션이 끝난 뒤에 화면을 띄우고 멈춘다
        private IEnumerator ShowAfterDeathMotion()
        {
            yield return new WaitForSeconds(_showDelay);

            _root.SetActive(true);
            Time.timeScale = 0f;
        }

        private void Retry()
        {
            SceneReloader.Reload(this);
        }
    }
}
