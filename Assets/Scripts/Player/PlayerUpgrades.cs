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
        // 근접 무기가 여러 자루라서 강화는 어느 것을 들든 적용되도록 전부에 준다
        private KnifeDamage[] _melee;
        private WeaponInventory _weapons;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _movement = GetComponent<FirstPersonMovement>();
            _weapons = GetComponent<WeaponInventory>();
            // 근접 무기는 카메라 아래 팔 오브젝트에 붙어 있고 꺼져 있을 수 있다
            _melee = GetComponentsInChildren<KnifeDamage>(true);

            if (_melee.Length == 0)
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
                    foreach (KnifeDamage melee in _melee)
                    {
                        melee.AddDamage(Mathf.RoundToInt(upgrade.Amount));
                    }

                    break;
                case UpgradeEffect.KnifeRange:
                    foreach (KnifeDamage melee in _melee)
                    {
                        melee.AddRange(upgrade.Amount);
                    }

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
