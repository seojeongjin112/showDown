using ArenaBreak.Core;
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

        [Header("난이도 선택")]
        [Tooltip("시작을 누르면 켜지는 난이도 버튼 묶음. 비워 두면 난이도를 묻지 않고 바로 시작한다")]
        [SerializeField] private GameObject _difficultyPanel;
        [Tooltip("시작·종료 버튼 묶음. 난이도를 고르는 동안 숨긴다")]
        [SerializeField] private GameObject _mainButtons;
        [SerializeField] private Button[] _difficultyButtons;
        [Tooltip("위 버튼과 같은 순서의 난이도")]
        [SerializeField] private DifficultyData[] _difficulties;
        [SerializeField] private Button _backButton;

        private UnityEngine.Events.UnityAction[] _difficultyHandlers;

        private void OnEnable()
        {
            _startButton.onClick.AddListener(OnStartPressed);
            _quitButton.onClick.AddListener(QuitGame);

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(CloseDifficulty);
            }

            if (_difficultyButtons != null)
            {
                _difficultyHandlers = new UnityEngine.Events.UnityAction[_difficultyButtons.Length];
                for (int i = 0; i < _difficultyButtons.Length; i++)
                {
                    int index = i;
                    _difficultyHandlers[i] = () => ChooseDifficulty(index);
                    _difficultyButtons[i].onClick.AddListener(_difficultyHandlers[i]);
                }
            }

            CloseDifficulty();
        }

        private void OnDisable()
        {
            _startButton.onClick.RemoveListener(OnStartPressed);
            _quitButton.onClick.RemoveListener(QuitGame);

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(CloseDifficulty);
            }

            if (_difficultyButtons != null && _difficultyHandlers != null)
            {
                for (int i = 0; i < _difficultyButtons.Length; i++)
                {
                    _difficultyButtons[i].onClick.RemoveListener(_difficultyHandlers[i]);
                }
            }
        }

        private void OnStartPressed()
        {
            if (_difficultyPanel == null)
            {
                StartGame();
                return;
            }

            _difficultyPanel.SetActive(true);
            if (_mainButtons != null)
            {
                _mainButtons.SetActive(false);
            }
        }

        private void CloseDifficulty()
        {
            if (_difficultyPanel != null)
            {
                _difficultyPanel.SetActive(false);
            }

            if (_mainButtons != null)
            {
                _mainButtons.SetActive(true);
            }
        }

        // 고른 난이도는 정적 값으로 게임 씬까지 이어진다
        private void ChooseDifficulty(int index)
        {
            Difficulty.Current = _difficulties != null && index < _difficulties.Length ? _difficulties[index] : null;
            StartGame();
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
