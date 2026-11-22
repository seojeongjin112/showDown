using System;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;
        [Tooltip("{0}은 이름, {1}은 가격")]
        [SerializeField] private string _labelFormat = "{0}\n코인 {1}";

        private UpgradeData _data;
        private Action<UpgradeData> _onBuy;

        public UpgradeData Data => _data;

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        public void Show(UpgradeData data, Action<UpgradeData> onBuy)
        {
            _data = data;
            _onBuy = onBuy;

            _label.text = string.Format(_labelFormat, data.DisplayName, data.Price);
        }

        // 살 수 없는 상품은 눌러도 되는 것처럼 보이지 않게 비활성으로 보여 준다
        public void Refresh(int coins, bool canApply)
        {
            _button.interactable = _data != null && canApply && coins >= _data.Price;
        }

        private void OnClicked()
        {
            _onBuy?.Invoke(_data);
        }
    }
}
