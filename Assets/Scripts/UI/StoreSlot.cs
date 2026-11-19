using System;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class StoreSlot : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _priceText;

        private StoreItemData _item;

        public StoreItemData Item => _item;

        public event Action<StoreItemData> Clicked;

        private void Awake()
        {
            if (_button == null || _icon == null || _nameText == null || _priceText == null)
            {
                Debug.LogError("StoreSlot: 버튼, 아이콘, 이름, 가격 참조가 비어 있습니다", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        // null이면 빈 칸이다. 상품이 6개보다 적을 때 남는 칸을 클릭할 수 없게 둔다
        public void Bind(StoreItemData item)
        {
            _item = item;
            bool hasItem = item != null;

            _button.interactable = hasItem;
            _icon.sprite = hasItem ? item.Icon : null;
            _icon.enabled = _icon.sprite != null;
            _nameText.text = hasItem ? item.DisplayName : string.Empty;
            _priceText.text = hasItem ? item.Price.ToString() : string.Empty;
        }

        private void OnButtonClicked()
        {
            if (_item != null)
            {
                Clicked?.Invoke(_item);
            }
        }
    }
}
