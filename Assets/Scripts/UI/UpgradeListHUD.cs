using System.Collections.Generic;
using ArenaBreak.Core;
using ArenaBreak.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    // 레벨업 때 고른 강화를 경험치 바 아래에 아이콘과 단계(레벨)로 보여 준다. 같은 강화를 또 고르면 단계만 오른다
    public class UpgradeListHUD : MonoBehaviour
    {
        [SerializeField] private PlayerUpgrades _upgrades;
        [Tooltip("위에서부터 채워지는 줄. 카드 종류 수만큼 둔다")]
        [SerializeField] private GameObject[] _rows;
        [SerializeField] private Image[] _icons;
        [SerializeField] private Text[] _labels;
        [Tooltip("{0}은 강화 이름, {1}은 단계")]
        [SerializeField] private string _labelFormat = "{0}  Lv{1}";

        private readonly Dictionary<UpgradeData, int> _rowOf = new Dictionary<UpgradeData, int>();

        private void OnEnable()
        {
            foreach (GameObject row in _rows)
            {
                row.SetActive(false);
            }

            _rowOf.Clear();
            _upgrades.Applied += OnApplied;
        }

        private void OnDisable()
        {
            if (_upgrades != null)
            {
                _upgrades.Applied -= OnApplied;
            }
        }

        private void OnApplied(UpgradeData upgrade, int level)
        {
            // 무기 해금·탄약 구입처럼 단계가 없는 효과는 목록에 올리지 않는다
            if (upgrade.Effect == UpgradeEffect.UnlockWeapon || upgrade.Effect == UpgradeEffect.AddAmmo || upgrade.Effect == UpgradeEffect.Heal)
            {
                return;
            }

            if (!_rowOf.TryGetValue(upgrade, out int index))
            {
                index = _rowOf.Count;
                if (index >= _rows.Length)
                {
                    return;
                }

                _rowOf[upgrade] = index;
                _icons[index].sprite = upgrade.Icon;
                _rows[index].SetActive(true);
            }

            _labels[index].text = string.Format(_labelFormat, upgrade.DisplayName, level);
        }
    }
}
