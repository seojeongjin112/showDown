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

        // 난이도가 플레이어가 받는 피해만 바꾸므로 기본값은 그대로 받는 1이다
        private float _incomingDamageMultiplier = 1f;

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

        // 난이도는 시작 체력 자체를 바꾸므로 피해를 입기 전에만 불러야 하고, 그래서 현재 체력도 가득 채운다
        public void ScaleMax(float multiplier)
        {
            if (IsDead || multiplier <= 0f)
            {
                return;
            }

            _maxHealth = Mathf.Max(1, Mathf.RoundToInt(_maxHealth * multiplier));
            Current = _maxHealth;
            Changed?.Invoke(Current, _maxHealth);
        }

        public void SetIncomingDamageMultiplier(float multiplier)
        {
            _incomingDamageMultiplier = Mathf.Max(0f, multiplier);
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

            // 배율을 곱해도 최소 1은 깎아야 약한 공격이 쉬운 난이도에서 아예 무시되지 않는다
            int scaled = Mathf.Max(1, Mathf.RoundToInt(amount * _incomingDamageMultiplier));

            LastDamageSource = source;
            Current = Mathf.Max(0, Current - scaled);
            Changed?.Invoke(Current, _maxHealth);

            if (IsDead)
            {
                Died?.Invoke();
            }
        }
    }
}
