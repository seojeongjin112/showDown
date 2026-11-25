using System.Collections.Generic;
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

        // 총·활·토마호크·다이너마이트에 곱하는 피해 배수. 근접 무기는 칼 피해 카드로 따로 오른다
        public float RangedDamageMultiplier { get; private set; } = 1f;
        public float ReloadSpeedMultiplier { get; private set; } = 1f;
        // 무기와 탄약이 떨어질 확률에 곱한다
        public float DropLuckMultiplier { get; private set; } = 1f;

        private readonly Dictionary<UpgradeData, int> _levels = new Dictionary<UpgradeData, int>();

        // HUD가 폴링하지 않고 구독해서 고른 강화 목록을 갱신하도록 강화와 그 강화의 현재 단계를 함께 넘긴다
        public event System.Action<UpgradeData, int> Applied;

        public int LevelOf(UpgradeData upgrade)
        {
            return _levels.TryGetValue(upgrade, out int level) ? level : 0;
        }

        // 피해는 정수라서 곱한 뒤의 소수점은 그 확률로 올림한다. 피해 1짜리 개틀링도 평균적으로 강화가 반영된다
        public int ScaleRangedDamage(int baseDamage)
        {
            return ScaleRangedDamage((float)baseDamage);
        }

        // 거리별로 줄어든 소수 피해도 같은 규칙(소수점은 확률로 올림)으로 정수로 만든다
        public int ScaleRangedDamage(float baseDamage)
        {
            float scaled = baseDamage * RangedDamageMultiplier;
            int whole = Mathf.FloorToInt(scaled);
            return Random.value < scaled - whole ? whole + 1 : whole;
        }

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
                case UpgradeEffect.GunDamage:
                    RangedDamageMultiplier += upgrade.Amount;
                    break;
                case UpgradeEffect.ReloadSpeed:
                    ReloadSpeedMultiplier += upgrade.Amount;
                    break;
                case UpgradeEffect.MaxAmmo:
                    _weapons.IncreaseReserveCap(upgrade.Amount);
                    break;
                case UpgradeEffect.DropLuck:
                    DropLuckMultiplier += upgrade.Amount;
                    break;
            }

            int level = LevelOf(upgrade) + 1;
            _levels[upgrade] = level;
            Applied?.Invoke(upgrade, level);
        }
    }
}
