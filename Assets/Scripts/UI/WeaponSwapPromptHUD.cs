using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    // 같은 종류의 무기를 주우려 할 때 "바꿀까요?"를 묻는 안내판. 묻는 동안에만 보이고, 두 무기의 그림을 나란히 보여 준다
    public class WeaponSwapPromptHUD : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _currentIcon;
        [SerializeField] private Image _newIcon;
        [SerializeField] private Text _message;
        [SerializeField] private Text _keys;
        [Tooltip("{0}은 지금 든 무기, {1}은 새 무기")]
        [SerializeField] private string _messageFormat = "{0}을(를) 내려놓고 {1}(으)로 바꿀까요?";
        [SerializeField] private string _keysText = "[E] 바꾸기    [Q] 그대로 두기";

        private void OnEnable()
        {
            _group.alpha = 0f;
            WeaponPickup.PendingChanged += OnPendingChanged;

            // 켜지기 전에 이미 묻는 중이었다면 이어서 보여 준다
            OnPendingChanged(WeaponPickup.Pending);
        }

        private void OnDisable()
        {
            WeaponPickup.PendingChanged -= OnPendingChanged;
        }

        private void OnPendingChanged(WeaponPickup pickup)
        {
            if (pickup == null || pickup.Weapon == null)
            {
                _group.alpha = 0f;
                return;
            }

            WeaponData current = pickup.ReplacedWeapon;
            _currentIcon.sprite = current != null ? current.Icon : null;
            _currentIcon.enabled = _currentIcon.sprite != null;
            _newIcon.sprite = pickup.Weapon.Icon;
            _newIcon.enabled = _newIcon.sprite != null;
            _message.text = string.Format(_messageFormat, current != null ? current.DisplayName : string.Empty, pickup.Weapon.DisplayName);
            _keys.text = _keysText;
            _group.alpha = 1f;
        }
    }
}
