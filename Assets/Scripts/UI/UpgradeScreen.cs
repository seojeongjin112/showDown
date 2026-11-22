using System.Collections.Generic;
using ArenaBreak.Core;
using ArenaBreak.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    // 레벨업 때 게임을 멈추고 무료 강화 카드 한 장 고르기와 코인 상점을 한 창에 보여 준다
    public class UpgradeScreen : MonoBehaviour
    {
        [SerializeField] private Experience _experience;
        [SerializeField] private CoinWallet _wallet;
        [SerializeField] private PlayerUpgrades _upgrades;
        [SerializeField] private GameObject _root;
        [SerializeField] private UpgradeData[] _cardPool;
        [SerializeField] private UpgradeCardView[] _cardViews;
        [SerializeField] private UpgradeData[] _shopItems;
        [SerializeField] private ShopItemView[] _shopViews;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _coinLabel;
        [SerializeField] private string _coinLabelFormat = "코인 {0}";
        // 시점·공격 입력을 꺼야 한다. 시점 스크립트는 끄면 커서가 풀리고 다시 켜면 잠긴다
        [SerializeField] private Behaviour[] _disableWhileOpen;
        // 커서가 풀린 동안 화면 중앙에 조준점이 남으면 마우스 포인터와 겹쳐 보인다
        [SerializeField] private GameObject[] _hideWhileOpen;

        private readonly List<UpgradeData> _rollBuffer = new List<UpgradeData>();
        private int _pendingPicks;
        private bool _isOpen;

        private void Awake()
        {
            _root.SetActive(false);

            for (int i = 0; i < _shopViews.Length; i++)
            {
                bool hasItem = i < _shopItems.Length;
                _shopViews[i].gameObject.SetActive(hasItem);

                if (hasItem)
                {
                    _shopViews[i].Show(_shopItems[i], Buy);
                }
            }
        }

        private void OnEnable()
        {
            _experience.LeveledUp += OnLeveledUp;
            _wallet.Changed += OnCoinsChanged;
            _closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            _experience.LeveledUp -= OnLeveledUp;
            _wallet.Changed -= OnCoinsChanged;
            _closeButton.onClick.RemoveListener(Close);

            // 열린 채로 꺼지면 시간이 멈추고 커서가 풀린 채로 남는다
            if (_isOpen)
            {
                _pendingPicks = 0;
                Close();
            }
        }

        private void OnLeveledUp(int level)
        {
            // 한 번에 여러 레벨이 오르면 고르기를 그만큼 이어서 한다
            _pendingPicks++;

            if (!_isOpen)
            {
                Open();
            }
        }

        private void Open()
        {
            _isOpen = true;

            foreach (Behaviour behaviour in _disableWhileOpen)
            {
                behaviour.enabled = false;
            }

            foreach (GameObject target in _hideWhileOpen)
            {
                target.SetActive(false);
            }

            _root.SetActive(true);
            Time.timeScale = 0f;

            RollCards();
            RefreshShop();
            RefreshClose();
        }

        private void Close()
        {
            // 레벨업 보상을 고르지 않고 닫으면 보상이 사라지므로 다 고를 때까지 닫지 못하게 한다
            if (_pendingPicks > 0)
            {
                return;
            }

            _isOpen = false;
            _root.SetActive(false);
            Time.timeScale = 1f;

            foreach (GameObject target in _hideWhileOpen)
            {
                target.SetActive(true);
            }

            foreach (Behaviour behaviour in _disableWhileOpen)
            {
                behaviour.enabled = true;
            }
        }

        // 뽑은 후보는 목록에서 빼야 같은 강화가 한 번에 두 장 나오지 않는다
        private void RollCards()
        {
            _rollBuffer.Clear();

            // 이미 가진 무기 카드는 골라도 아무 효과가 없으므로 후보에서 뺀다
            foreach (UpgradeData card in _cardPool)
            {
                if (_upgrades.CanApply(card))
                {
                    _rollBuffer.Add(card);
                }
            }

            for (int i = 0; i < _cardViews.Length; i++)
            {
                if (_rollBuffer.Count == 0)
                {
                    _cardViews[i].gameObject.SetActive(false);
                    continue;
                }

                int pick = Random.Range(0, _rollBuffer.Count);
                _cardViews[i].Show(_rollBuffer[pick], OnCardChosen);
                _cardViews[i].SetInteractable(true);
                _rollBuffer.RemoveAt(pick);
            }
        }

        private void OnCardChosen(UpgradeData upgrade)
        {
            _upgrades.Apply(upgrade);
            _pendingPicks--;

            // 방금 얻은 무기의 탄약을 상점에서 바로 살 수 있어야 하므로 카드 선택 뒤에도 상점을 다시 그린다
            RefreshShop();

            if (_pendingPicks > 0)
            {
                RollCards();
            }
            else
            {
                foreach (UpgradeCardView card in _cardViews)
                {
                    card.SetInteractable(false);
                }
            }

            RefreshClose();
        }

        private void Buy(UpgradeData item)
        {
            if (!_upgrades.CanApply(item))
            {
                return;
            }

            if (_wallet.TrySpend(item.Price))
            {
                _upgrades.Apply(item);

                // 코인이 줄 때의 갱신은 효과를 적용하기 전이라, 적용 뒤의 체력 상태를 반영하려면 한 번 더 그린다
                RefreshShop();
            }
        }

        private void OnCoinsChanged(int coins)
        {
            if (_isOpen)
            {
                RefreshShop();
            }
        }

        private void RefreshShop()
        {
            int coins = _wallet.Count;
            _coinLabel.text = string.Format(_coinLabelFormat, coins);

            foreach (ShopItemView view in _shopViews)
            {
                view.Refresh(coins, view.Data != null && _upgrades.CanApply(view.Data));
            }
        }

        private void RefreshClose()
        {
            _closeButton.interactable = _pendingPicks == 0;
        }
    }
}
