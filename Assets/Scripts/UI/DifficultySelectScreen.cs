using System;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class DifficultySelectScreen : MonoBehaviour
    {
        [Serializable]
        private class Option
        {
            [SerializeField] private Button _button;
            [SerializeField] private DifficultyData _difficulty;

            public Button Button => _button;
            public DifficultyData Difficulty => _difficulty;
        }

        [SerializeField] private GameObject _root;
        // 단계 수가 정해지지 않아서, 버튼과 데이터를 짝지어 원하는 만큼 늘리고 줄일 수 있게 한다
        [SerializeField] private Option[] _options;
        [SerializeField] private Button _backButton;
        [SerializeField] private string _gameSceneName = "Game";

        // 타이틀 버튼을 다시 보여 주는 일은 TitleScreen의 책임이라 이벤트로만 알린다
        public event Action BackRequested;

        private UnityEngine.Events.UnityAction[] _optionHandlers;

        private void Awake()
        {
            _optionHandlers = new UnityEngine.Events.UnityAction[_options.Length];
            for (int i = 0; i < _options.Length; i++)
            {
                DifficultyData difficulty = _options[i].Difficulty;
                _optionHandlers[i] = () => Choose(difficulty);

                // 버튼 글자를 데이터에서 가져와 이름을 두 군데서 고치지 않게 한다
                Text label = _options[i].Button.GetComponentInChildren<Text>(true);
                if (label != null && difficulty != null)
                {
                    label.text = difficulty.DisplayName;
                }
            }

            _root.SetActive(false);
        }

        private void OnEnable()
        {
            for (int i = 0; i < _options.Length; i++)
            {
                _options[i].Button.onClick.AddListener(_optionHandlers[i]);
            }

            _backButton.onClick.AddListener(Back);
        }

        private void OnDisable()
        {
            for (int i = 0; i < _options.Length; i++)
            {
                _options[i].Button.onClick.RemoveListener(_optionHandlers[i]);
            }

            _backButton.onClick.RemoveListener(Back);
        }

        public void Open()
        {
            _root.SetActive(true);
        }

        private void Back()
        {
            _root.SetActive(false);
            BackRequested?.Invoke();
        }

        private void Choose(DifficultyData difficulty)
        {
            if (difficulty == null)
            {
                Debug.LogError("DifficultySelectScreen: 버튼에 난이도 데이터가 연결되지 않았습니다", this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(_gameSceneName))
            {
                Debug.LogError($"DifficultySelectScreen: '{_gameSceneName}' 씬이 Build Profiles의 Scene List에 없어서 불러올 수 없습니다", this);
                return;
            }

            DifficultySelection.Select(difficulty);

            // 게임오버 화면이 timeScale을 0으로 둔 채 타이틀로 돌아오는 경우를 대비해 되돌린다
            Time.timeScale = 1f;
            SceneManager.LoadScene(_gameSceneName);
        }
    }
}
