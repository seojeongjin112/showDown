using ArenaBreak.Core;
using ArenaBreak.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class AmmoHUD : MonoBehaviour
    {
        [SerializeField] private WeaponInventory _inventory;
        [Tooltip("탄약을 쓰는 무기를 들었을 때만 켜지는 패널")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _label;
        [Tooltip("{0}은 탄창, {1}은 예비 탄약")]
        [SerializeField] private string _labelFormat = "{0} / {1}";

        private WeaponAmmo _current;

        private void OnEnable()
        {
            if (_inventory == null)
            {
                return;
            }

            _inventory.SelectionChanged += OnSelectionChanged;
            Track(_inventory.SelectedIndex);
        }

        private void OnDisable()
        {
            if (_inventory == null)
            {
                return;
            }

            _inventory.SelectionChanged -= OnSelectionChanged;
            Untrack();
        }

        private void OnSelectionChanged(int index)
        {
            Track(index);
        }

        // 들고 있는 무기가 바뀔 때마다 그 무기의 탄약만 구독해야 다른 무기의 변화가 화면에 끼어들지 않는다
        private void Track(int index)
        {
            Untrack();

            _current = _inventory.AmmoAt(index);
            _panel.SetActive(_current != null);

            if (_current == null)
            {
                return;
            }

            _current.Changed += OnAmmoChanged;
            _icon.sprite = _inventory.WeaponAt(index).AmmoIcon;
            OnAmmoChanged(_current.Magazine, _current.Reserve);
        }

        private void Untrack()
        {
            if (_current != null)
            {
                _current.Changed -= OnAmmoChanged;
                _current = null;
            }
        }

        private void OnAmmoChanged(int magazine, int reserve)
        {
            _label.text = string.Format(_labelFormat, magazine, reserve);
        }
    }
}
