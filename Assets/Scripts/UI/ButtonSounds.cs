using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    // 버튼마다 소리 컴포넌트를 붙이지 않고, 캔버스 하나에 붙여 아래의 버튼을 모두 찾아 클릭 소리를 단다
    public class ButtonSounds : MonoBehaviour
    {
        [SerializeField] private AudioClip _click;
        [Tooltip("뒤로·닫기처럼 물러나는 버튼은 다른 소리를 낸다")]
        [SerializeField] private AudioClip _back;
        [SerializeField] private Button[] _backButtons;
        [Tooltip("강화 카드 선택처럼 결정하는 버튼은 다른 소리를 낸다")]
        [SerializeField] private AudioClip _confirm;
        [SerializeField] private Button[] _confirmButtons;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.7f;

        private Button[] _buttons;
        private UnityEngine.Events.UnityAction[] _handlers;

        private void Awake()
        {
            // 꺼져 있는 패널(난이도·게임오버·강화) 안의 버튼까지 미리 찾아 둔다
            _buttons = GetComponentsInChildren<Button>(true);
            _handlers = new UnityEngine.Events.UnityAction[_buttons.Length];

            for (int i = 0; i < _buttons.Length; i++)
            {
                AudioClip clip = PickClip(_buttons[i]);
                _handlers[i] = () => SfxPlayer.Play2D(clip, _volume);
            }
        }

        private void OnEnable()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].onClick.AddListener(_handlers[i]);
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].onClick.RemoveListener(_handlers[i]);
            }
        }

        private AudioClip PickClip(Button button)
        {
            if (_back != null && Contains(_backButtons, button))
            {
                return _back;
            }

            if (_confirm != null && Contains(_confirmButtons, button))
            {
                return _confirm;
            }

            return _click;
        }

        private static bool Contains(Button[] list, Button button)
        {
            if (list == null)
            {
                return false;
            }

            foreach (Button item in list)
            {
                if (item == button)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
