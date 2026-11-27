using System.Collections.Generic;
using ArenaBreak.Core;
using ArenaBreak.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    // 레벨업 때 게임을 멈추고 무료 강화 카드 한 장을 고르게 한다
    public class UpgradeScreen : MonoBehaviour
    {
        [SerializeField] private Experience _experience;
        [SerializeField] private PlayerUpgrades _upgrades;
        [SerializeField] private GameObject _root;
        [SerializeField] private UpgradeData[] _cardPool;
        [SerializeField] private UpgradeCardView[] _cardViews;
        [SerializeField] private Button _closeButton;
        // 시점·공격 입력을 꺼야 한다. 시점 스크립트는 끄면 커서가 풀리고 다시 켜면 잠긴다
        [SerializeField] private Behaviour[] _disableWhileOpen;
        // 커서가 풀린 동안 화면 중앙에 조준점이 남으면 마우스 포인터와 겹쳐 보인다
        [SerializeField] private GameObject[] _hideWhileOpen;

        private readonly List<UpgradeData> _rollBuffer = new List<UpgradeData>();
        private int _pendingPicks;
        private bool _isOpen;
        // 결과 화면처럼 다른 쪽이 먼저 꺼 둔 조작을 창을 닫으며 다시 켜지 않도록, 열 때 켜져 있던 것만 기억한다
        private readonly System.Collections.Generic.List<Behaviour> _disabledByMe = new System.Collections.Generic.List<Behaviour>();

        private void Awake()
        {
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            _experience.LeveledUp += OnLeveledUp;
            _closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            _experience.LeveledUp -= OnLeveledUp;
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

            _disabledByMe.Clear();
            foreach (Behaviour behaviour in _disableWhileOpen)
            {
                if (behaviour != null && behaviour.enabled)
                {
                    behaviour.enabled = false;
                    _disabledByMe.Add(behaviour);
                }
            }

            foreach (GameObject target in _hideWhileOpen)
            {
                target.SetActive(false);
            }

            _root.SetActive(true);
            Time.timeScale = 0f;

            RollCards();
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

            // 열린 채로 플레이를 끝내면 대상이 먼저 파괴되어 있을 수 있어서 비어 있는지 본다
            foreach (GameObject target in _hideWhileOpen)
            {
                if (target != null)
                {
                    target.SetActive(true);
                }
            }

            foreach (Behaviour behaviour in _disabledByMe)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = true;
                }
            }

            _disabledByMe.Clear();
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

        private void RefreshClose()
        {
            _closeButton.interactable = _pendingPicks == 0;
        }
    }
}
