using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private string _gameSceneName = "Game";

        private void OnEnable()
        {
            _startButton.onClick.AddListener(StartGame);
            _quitButton.onClick.AddListener(QuitGame);
        }

        private void OnDisable()
        {
            _startButton.onClick.RemoveListener(StartGame);
            _quitButton.onClick.RemoveListener(QuitGame);
        }

        private void StartGame()
        {
            if (!Application.CanStreamedLevelBeLoaded(_gameSceneName))
            {
                Debug.LogError($"TitleScreen: '{_gameSceneName}' 씬이 Build Profiles의 Scene List에 없어서 불러올 수 없습니다", this);
                return;
            }

            // 게임오버 화면이 timeScale을 0으로 둔 채 타이틀로 돌아오는 경우를 대비해 되돌린다
            Time.timeScale = 1f;
            SceneManager.LoadScene(_gameSceneName);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            // 에디터에서는 Application.Quit이 무시되므로 종료 버튼을 테스트할 수 있게 플레이 모드를 끈다
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
