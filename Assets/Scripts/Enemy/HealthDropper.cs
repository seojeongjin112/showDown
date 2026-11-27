using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 근접으로 쓰러뜨리면 체력을 많이, 멀리서 쏴서 잡으면 조금만 떨어뜨린다. 위험을 무릅쓰고 붙은 쪽에 보상을 준다
    [RequireComponent(typeof(Health))]
    public class HealthDropper : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [SerializeField] private HealthPickup _pickupPrefab;

        [Header("근접으로 처치했을 때")]
        [SerializeField, Range(0f, 1f)] private float _meleeChance = 0.1f;
        [SerializeField] private int _meleeHeal = 2;

        [Header("그 외 무기로 처치했을 때")]
        [SerializeField, Range(0f, 1f)] private float _otherChance = 0.03f;
        [SerializeField] private int _otherHeal = 1;

        [Tooltip("0이면 체력과 상관없이 위 확률 그대로. 값을 주면 체력을 많이 잃었을수록 확률이 최대 이 비율만큼 늘어난다")]
        [SerializeField, Range(0f, 1f)] private float _lowHealthBonus;
        [SerializeField] private float _scatterRadius = 1.2f;
        // 가슴 높이에서 튀어나오게 해 발밑에서 바로 솟아 보이지 않게 한다
        [SerializeField] private float _spawnHeight = 1f;
        [SerializeField] private float _snapDistance = 1.5f;

        private Health _health;
        private Health _playerHealth;

        private void Awake()
        {
            _health = GetComponent<Health>();

            GameObject player = GameObject.FindWithTag(PlayerTag);
            if (_pickupPrefab == null || player == null || !player.TryGetComponent(out _playerHealth))
            {
                Debug.LogError("HealthDropper: 픽업 프리팹, 또는 Player의 Health를 찾지 못했습니다", this);
                enabled = false;
            }
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
            bool melee = _health.LastDamageSource == DamageSource.Melee;
            float chance = (melee ? _meleeChance : _otherChance) * Difficulty.HealDrop;
            int heal = melee ? _meleeHeal : _otherHeal;

            float missing = 1f - (float)_playerHealth.Current / _playerHealth.Max;
            chance *= 1f + _lowHealthBonus * missing;

            if (heal <= 0 || _playerHealth.IsDead || Random.value >= chance)
            {
                return;
            }

            Vector3 origin = transform.position;
            Vector3 landing = Pickup.ScatterPoint(origin, _scatterRadius, _snapDistance);

            HealthPickup pickup = Instantiate(_pickupPrefab, origin + Vector3.up * _spawnHeight, Quaternion.identity);
            pickup.Setup(heal);
            pickup.Launch(landing);
        }
    }
}
