using System;
using UnityEngine;

namespace ArenaBreak.Core
{
    // 어떤 공격이 마지막 일격이었는지 알려, 근접으로 처치했을 때만 탄약을 떨어뜨리는 규칙을 가능하게 한다
    public enum DamageSource
    {
        Unknown,
        Melee
    }

    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private int _maxHealth = 10;
        [Tooltip("보스(보스 루트와 각 단계)는 난이도의 보스 체력 배율을 한 번 더 받는다")]
        [SerializeField] private bool _isBoss;

        public int Current { get; private set; }
        public int Max => _maxHealth;
        public bool IsDead => Current <= 0;
        public DamageSource LastDamageSource { get; private set; }
        public bool IsBoss => _isBoss;

        // 기록판처럼 모든 죽음을 세야 하는 쪽이 적마다 구독하지 않고 한 곳에서 듣는다
        public static event Action<Health> AnyDied;

        // HUD가 폴링하지 않고 구독해서 갱신하도록 현재/최대를 함께 넘긴다
        public event Action<int, int> Changed;
        public event Action Died;

        private void Awake()
        {
            ApplyDifficulty();
            Current = _maxHealth;
        }

        // 난이도는 체력이 정해지기 전에 한 번만 반영한다. 보스 루트와 단계가 같은 값을 받아야 체력바가 맞는다
        private void ApplyDifficulty()
        {
            if (CompareTag("Player"))
            {
                if (Difficulty.PlayerMaxHealth > 0)
                {
                    _maxHealth = Difficulty.PlayerMaxHealth;
                }

                return;
            }

            float multiplier = Difficulty.EnemyHealth * (_isBoss ? Difficulty.BossHealth : 1f);
            if (!Mathf.Approximately(multiplier, 1f))
            {
                _maxHealth = Difficulty.Scale(_maxHealth, multiplier);
            }
        }

        // 최대치만 올리고 현재 체력은 그대로 두면 강화가 오히려 체력바를 비어 보이게 하므로 올린 만큼 채운다
        public void IncreaseMax(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            _maxHealth += amount;
            Current += amount;
            Changed?.Invoke(Current, _maxHealth);
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            Current = Mathf.Min(_maxHealth, Current + amount);
            Changed?.Invoke(Current, _maxHealth);
        }

        public void TakeDamage(int amount)
        {
            TakeDamage(amount, DamageSource.Unknown);
        }

        public void TakeDamage(int amount, DamageSource source)
        {
            // 사망 뒤에 들어온 피해로 Died가 두 번 나가면 사망 처리가 중복된다
            if (IsDead || amount <= 0)
            {
                return;
            }

            // 플레이어가 받는 피해만 난이도 배율을 받는다
            if (CompareTag("Player"))
            {
                amount = Difficulty.ScaleDamageTaken(amount);
            }

            LastDamageSource = source;
            Current = Mathf.Max(0, Current - amount);
            Changed?.Invoke(Current, _maxHealth);

            if (IsDead)
            {
                // 기록판이 먼저 세야 한다. 마지막 보스의 Died로 승리 화면이 기록을 멈추면 그 처치가 빠진다
                AnyDied?.Invoke(this);
                Died?.Invoke();
            }
        }
    }
}
