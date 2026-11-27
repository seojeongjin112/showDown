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

        public int Current { get; private set; }
        public int Max => _maxHealth;
        public bool IsDead => Current <= 0;
        public DamageSource LastDamageSource { get; private set; }

        // HUD가 폴링하지 않고 구독해서 갱신하도록 현재/최대를 함께 넘긴다
        public event Action<int, int> Changed;
        public event Action Died;

        private void Awake()
        {
            Current = _maxHealth;
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

            LastDamageSource = source;
            Current = Mathf.Max(0, Current - amount);
            Changed?.Invoke(Current, _maxHealth);

            if (IsDead)
            {
                Died?.Invoke();
            }
        }
    }
}
