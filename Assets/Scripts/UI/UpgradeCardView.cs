using System;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class UpgradeCardView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _name;
        [SerializeField] private Text _description;

        private UpgradeData _data;
        private Action<UpgradeData> _onChosen;

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        public void Show(UpgradeData data, Action<UpgradeData> onChosen)
        {
            _data = data;
            _onChosen = onChosen;

            _icon.sprite = data.Icon;
            _name.text = data.DisplayName;
            _description.text = data.Description;
            gameObject.SetActive(true);
        }

        public void SetInteractable(bool value)
        {
            _button.interactable = value;
        }

        private void OnClicked()
        {
            _onChosen?.Invoke(_data);
        }
    }
}
