using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaBreak.Core
{
    // 연습 씬 전용. 튜토리얼을 끝내거나 건너뛰면 완료 안내를 잠깐 보여 준 뒤 타이틀로 돌아간다
    public class PracticeExit : MonoBehaviour
    {
        [SerializeField] private TutorialManager _tutorial;
        [SerializeField] private string _titleSceneName = "Title";
        [Tooltip("완료 안내를 보여 주는 시간")]
        [SerializeField] private float _delaySeconds = 3f;
        [Tooltip("끝났을 때 켤 완료 안내(선택)")]
        [SerializeField] private GameObject _completeBanner;

        private void Awake()
        {
            if (_tutorial == null)
            {
                Debug.LogError("PracticeExit: 튜토리얼 매니저 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            if (_completeBanner != null)
            {
                _completeBanner.SetActive(false);
            }
        }

        private void OnEnable()
        {
            _tutorial.Finished += OnFinished;
        }

        private void OnDisable()
        {
            _tutorial.Finished -= OnFinished;
        }

        private void OnFinished()
        {
            if (_completeBanner != null)
            {
                _completeBanner.SetActive(true);
            }

            StartCoroutine(ReturnToTitle());
        }

        // 게임오버 화면처럼 시간이 멈춰 있어도 돌아갈 수 있게 실제 시간으로 기다린다
        private IEnumerator ReturnToTitle()
        {
            yield return new WaitForSecondsRealtime(_delaySeconds);
            Time.timeScale = 1f;
            SceneManager.LoadScene(_titleSceneName);
        }
    }
}
