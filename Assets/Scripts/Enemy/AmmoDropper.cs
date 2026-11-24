using ArenaBreak.Core;
using ArenaBreak.Player;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 근접으로 쓰러뜨린 적만 탄약을 떨어뜨린다. 멀리서 쏴서 잡으면 탄약이 안 나와서 가까이 붙을 이유가 생긴다
    [RequireComponent(typeof(Health))]
    public class AmmoDropper : MonoBehaviour
    {
        private const string PlayerTag = "Player";
        // 탄약이 거의 찬 무기도 가끔은 나오게 하는 최소 비중
        private const float MinimumWeight = 0.25f;

        [SerializeField] private AmmoPickup _pickupPrefab;
        [Tooltip("근접으로 처치했을 때 탄약을 떨어뜨릴 확률. 강한 적일수록 높게 둔다")]
        [SerializeField, Range(0f, 1f)] private float _dropChance = 0.25f;
        [Tooltip("근접이 아닌 무기로 처치했을 때 확률에 곱하는 값. 0이면 근접으로 잡아야만 나온다. 근접으로 잡기 어려운 단단한 적만 올려 둔다")]
        [SerializeField, Range(0f, 1f)] private float _otherKillMultiplier;
        [SerializeField] private float _scatterRadius = 1.2f;
        // 가슴 높이에서 튀어나오게 해 발밑에서 바로 솟아 보이지 않게 한다
        [SerializeField] private float _spawnHeight = 1f;
        [SerializeField] private float _snapDistance = 1.5f;

        private Health _health;
        private WeaponInventory _inventory;
        private PlayerUpgrades _upgrades;

        private void Awake()
        {
            _health = GetComponent<Health>();

            GameObject player = GameObject.FindWithTag(PlayerTag);
            if (_pickupPrefab == null || player == null || !player.TryGetComponent(out _inventory))
            {
                Debug.LogError("AmmoDropper: 픽업 프리팹, 또는 Player의 WeaponInventory를 찾지 못했습니다", this);
                enabled = false;
                return;
            }

            player.TryGetComponent(out _upgrades);
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
        }

        private void OnDied()
        {
            float chance = _health.LastDamageSource == DamageSource.Melee ? _dropChance : _dropChance * _otherKillMultiplier;
            // 드랍 운 카드가 있으면 확률이 그만큼 오른다
            chance *= _upgrades != null ? _upgrades.DropLuckMultiplier : 1f;
            if (Random.value >= chance)
            {
                return;
            }

            WeaponData weapon = PickWeapon();
            if (weapon == null)
            {
                return;
            }

            Vector3 origin = transform.position;
            Vector3 landing = Pickup.ScatterPoint(origin, _scatterRadius, _snapDistance);

            AmmoPickup pickup = Instantiate(_pickupPrefab, origin + Vector3.up * _spawnHeight, Quaternion.identity);
            pickup.Setup(weapon);
            pickup.Launch(landing);
        }

        // 지금 든 탄약 무기 중에서 예비 탄약이 더 비어 있는 쪽을 더 자주 고른다
        private WeaponData PickWeapon()
        {
            float total = 0f;
            for (int i = 0; i < _inventory.Count; i++)
            {
                total += WeightOf(i);
            }

            if (total <= 0f)
            {
                return null;
            }

            float roll = Random.value * total;
            WeaponData last = null;

            for (int i = 0; i < _inventory.Count; i++)
            {
                float weight = WeightOf(i);
                if (weight <= 0f)
                {
                    continue;
                }

                last = _inventory.WeaponAt(i);
                roll -= weight;
                if (roll <= 0f)
                {
                    return last;
                }
            }

            // 부동소수점 오차로 끝까지 못 고르면 마지막 후보를 준다
            return last;
        }

        private float WeightOf(int index)
        {
            WeaponData weapon = _inventory.WeaponAt(index);
            if (!_inventory.IsOwned(index) || !weapon.UsesAmmo || !_inventory.CanAddAmmo(weapon))
            {
                return 0f;
            }

            WeaponAmmo ammo = _inventory.AmmoAt(index);
            float missing = 1f - (float)ammo.Reserve / ammo.MaxReserve;
            return MinimumWeight + missing;
        }
    }
}
