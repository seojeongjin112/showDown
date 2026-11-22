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

        private void Awake()
        {
            _health = GetComponent<Health>();
            _movement = GetComponent<FirstPersonMovement>();
            // 칼은 카메라 아래 팔 오브젝트에 붙어 있다
            _knife = GetComponentInChildren<KnifeDamage>(true);

            if (_knife == null)
            {
                Debug.LogError("PlayerUpgrades: 자식에서 KnifeDamage를 찾지 못했습니다", this);
                enabled = false;
            }
        }

        // 가득 찬 체력에 회복을 사면 코인만 사라지므로 상점이 사지 못하게 막는다
        public bool CanApply(UpgradeData upgrade)
        {
            return upgrade.Effect != UpgradeEffect.Heal || _health.Current < _health.Max;
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
            }
        }
    }
}
