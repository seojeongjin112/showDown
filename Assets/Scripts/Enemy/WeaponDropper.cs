using ArenaBreak.Core;
using ArenaBreak.Player;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    [RequireComponent(typeof(Health))]
    public class WeaponDropper : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [Tooltip("스테이지에 표가 없을 때만 쓰는 예비 표")]
        [SerializeField] private WeaponDropTable _fallbackTable;
        [SerializeField] private WeaponPickup _pickupPrefab;
        [Tooltip("이 적이 죽을 때 무기를 떨어뜨릴 확률. 강한 적일수록 높게 둔다")]
        [SerializeField, Range(0f, 1f)] private float _dropChance = 0.04f;
        [Tooltip("1이면 등급 비율 그대로. 클수록 희귀·전설 등급이 더 잘 나온다. 강한 적에게 준다")]
        [SerializeField, Min(1f)] private float _rarityBoost = 1f;
        [SerializeField] private float _scatterRadius = 1.2f;
        // 가슴 높이에서 튀어나오게 해 발밑에서 바로 솟아 보이지 않게 한다
        [SerializeField] private float _spawnHeight = 1f;
        [SerializeField] private float _snapDistance = 1.5f;

        // 적이 많이 생겨도 씬 전체 검색이 반복되지 않도록 한 번만 찾아 둔다
        private static StageManager _stageManager;

        private Health _health;
        private WeaponInventory _inventory;
        private PlayerUpgrades _upgrades;

        private void Awake()
        {
            _health = GetComponent<Health>();

            GameObject player = GameObject.FindWithTag(PlayerTag);
            if (_pickupPrefab == null || player == null || !player.TryGetComponent(out _inventory))
            {
                Debug.LogError("WeaponDropper: 픽업 프리팹, 또는 Player의 WeaponInventory를 찾지 못했습니다", this);
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
            WeaponDropTable table = CurrentTable();
            // 드랍 운 카드가 있으면 확률이 그만큼 오른다
            float chance = Mathf.Min(1f, _dropChance * (_upgrades != null ? _upgrades.DropLuckMultiplier : 1f));
            if (table == null || !table.TryRoll(chance, _rarityBoost, _inventory, out WeaponDrop drop))
            {
                return;
            }

            Vector3 origin = transform.position;
            Vector3 landing = Pickup.ScatterPoint(origin, _scatterRadius, _snapDistance);

            WeaponPickup pickup = Instantiate(_pickupPrefab, origin + Vector3.up * _spawnHeight, Quaternion.identity);
            pickup.Setup(drop);
            pickup.Launch(landing);
        }

        // 스테이지가 바뀌면 표도 바뀌어야 하므로 죽는 순간에 현재 스테이지의 표를 본다
        private WeaponDropTable CurrentTable()
        {
            if (_stageManager == null)
            {
                _stageManager = FindFirstObjectByType<StageManager>();
            }

            StageData stage = _stageManager != null ? _stageManager.CurrentData : null;
            return stage != null && stage.WeaponDrops != null ? stage.WeaponDrops : _fallbackTable;
        }
    }
}
