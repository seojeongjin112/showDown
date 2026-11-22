using UnityEngine;

namespace ArenaBreak.Core
{
    public enum UpgradeEffect
    {
        MaxHealth,
        Heal,
        MoveSpeed,
        KnifeDamage,
        KnifeRange
    }

    [CreateAssetMenu(fileName = "Upgrade", menuName = "ArenaBreak/Upgrade")]
    public class UpgradeData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private UpgradeEffect _effect;
        // 체력·피해는 정수지만 속도·범위는 소수라 하나의 값으로 받는다
        [SerializeField] private float _amount = 1f;
        // 레벨업 보상 카드는 무료이고, 상점에서만 가격이 쓰인다
        [SerializeField] private int _price;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        public UpgradeEffect Effect => _effect;
        public float Amount => _amount;
        public int Price => _price;
    }
}
