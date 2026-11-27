using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;
        // 난이도 패널이 떠 있는 동안 뒤에서 시작·종료 버튼이 눌리지 않게 함께 숨긴다
        [SerializeField] private GameObject _titleButtons;
        [SerializeField] private DifficultySelectScreen _difficultyScreen;

        private void OnEnable()
        {
            _startButton.onClick.AddListener(OpenDifficultySelect);
            _quitButton.onClick.AddListener(QuitGame);
            _difficultyScreen.BackRequested += ShowTitleButtons;
        }

        private void OnDisable()
        {
            _startButton.onClick.RemoveListener(OpenDifficultySelect);
            _quitButton.onClick.RemoveListener(QuitGame);
            _difficultyScreen.BackRequested -= ShowTitleButtons;
        }

        private void OpenDifficultySelect()
        {
            _titleButtons.SetActive(false);
            _difficultyScreen.Open();
        }

        private void ShowTitleButtons()
        {
            _titleButtons.SetActive(true);
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
