using ArenaBreak.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class WeaponBarHUD : MonoBehaviour
    {
        [SerializeField] private WeaponInventory _inventory;
        [Tooltip("인벤토리 슬롯과 같은 순서의 칸 틀")]
        [SerializeField] private Image[] _frames;
        [Tooltip("인벤토리 슬롯과 같은 순서의 무기 그림")]
        [SerializeField] private Image[] _icons;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _selectedSprite;
        [SerializeField] private Sprite _lockedSprite;

        private void OnEnable()
        {
            if (_inventory == null)
            {
                return;
            }

            _inventory.SelectionChanged += OnChanged;
            _inventory.OwnedChanged += OnChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_inventory == null)
            {
                return;
            }

            _inventory.SelectionChanged -= OnChanged;
            _inventory.OwnedChanged -= OnChanged;
        }

        private void OnChanged(int index)
        {
            Refresh();
        }

        // 인벤토리는 시작 시 이벤트를 쏘지 않으므로 OnEnable에서 현재 상태로 한 번 그린다
        private void Refresh()
        {
            for (int i = 0; i < _frames.Length; i++)
            {
                bool owned = _inventory.IsOwned(i);

                if (!owned)
                {
                    _frames[i].sprite = _lockedSprite;
                }
                else
                {
                    _frames[i].sprite = i == _inventory.SelectedIndex ? _selectedSprite : _normalSprite;
                }

                // 못 가진 무기는 그림을 숨겨 무엇인지 모른 채 잠긴 칸으로만 보이게 한다
                _icons[i].enabled = owned;
            }
        }
    }
}
