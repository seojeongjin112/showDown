using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(FirstPersonMovement))]
    public class PlayerUpgrades : MonoBehaviour
    {
        private Health _health;
        private FirstPersonMovement _movement;
        private KnifeDamage _knife;
        private WeaponInventory _weapons;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _movement = GetComponent<FirstPersonMovement>();
            _weapons = GetComponent<WeaponInventory>();
            // 칼은 카메라 아래 팔 오브젝트에 붙어 있다
            _knife = GetComponentInChildren<KnifeDamage>(true);

            if (_knife == null)
            {
                Debug.LogError("PlayerUpgrades: 자식에서 KnifeDamage를 찾지 못했습니다", this);
                enabled = false;
            }
        }

        // 효과가 없는데 코인만 사라지는 구매(가득 찬 체력, 이미 가진 무기, 가득 찬 예비 탄약)는 상점이 막는다
        public bool CanApply(UpgradeData upgrade)
        {
            switch (upgrade.Effect)
            {
                case UpgradeEffect.Heal:
                    return _health.Current < _health.Max;
                case UpgradeEffect.UnlockWeapon:
                    return _weapons != null && _weapons.CanUnlock(upgrade.Weapon);
                case UpgradeEffect.AddAmmo:
                    return _weapons != null && _weapons.CanAddAmmo(upgrade.Weapon);
                default:
                    return true;
            }
        }

        public void Apply(UpgradeData upgrade)
        {
            switch (upgrade.Effect)
            {
                case UpgradeEffect.MaxHealth:
                    _health.IncreaseMax(Mathf.RoundToInt(upgrade.Amount));
                    break;
                case UpgradeEffect.Heal:
                    _health.Heal(Mathf.RoundToInt(upgrade.Amount));
                    break;
                case UpgradeEffect.MoveSpeed:
                    _movement.AddMoveSpeed(upgrade.Amount);
                    break;
                case UpgradeEffect.KnifeDamage:
                    _knife.AddDamage(Mathf.RoundToInt(upgrade.Amount));
                    break;
                case UpgradeEffect.KnifeRange:
                    _knife.AddRange(upgrade.Amount);
                    break;
                case UpgradeEffect.UnlockWeapon:
                    _weapons.Unlock(upgrade.Weapon);
                    break;
                case UpgradeEffect.AddAmmo:
                    _weapons.AddAmmo(upgrade.Weapon, Mathf.RoundToInt(upgrade.Amount));
                    break;
            }
        }
    }
}
