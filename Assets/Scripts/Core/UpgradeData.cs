using UnityEngine;

namespace ArenaBreak.Core
{
    public enum UpgradeEffect
    {
        MaxHealth,
        Heal,
        MoveSpeed,
        KnifeDamage,
        KnifeRange,
        // 저장된 에셋의 값이 밀리지 않도록 기존 항목 뒤에만 추가한다
        UnlockWeapon,
        AddAmmo,
        // 아래는 총과 탄약, 드랍을 강화하는 레벨업 카드용이다
        GunDamage,
        ReloadSpeed,
        MaxAmmo,
        DropLuck
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
        [Tooltip("무기 해금과 탄약 구매일 때 대상이 되는 무기")]
        [SerializeField] private WeaponData _weapon;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        public UpgradeEffect Effect => _effect;
        public float Amount => _amount;
        public int Price => _price;
        public WeaponData Weapon => _weapon;
    }
}
