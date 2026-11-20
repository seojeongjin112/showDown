using System;
using UnityEngine;

namespace ArenaBreak.Core
{
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private int _maxHealth = 10;

        public int Current { get; private set; }
        public int Max => _maxHealth;
        public bool IsDead => Current <= 0;

        // HUD가 폴링하지 않고 구독해서 갱신하도록 현재/최대를 함께 넘긴다
        public event Action<int, int> Changed;
        public event Action Died;

        private void Awake()
        {
            Current = _maxHealth;
        }

        public void TakeDamage(int amount)
        {
            // 사망 뒤에 들어온 피해로 Died가 두 번 나가면 사망 처리가 중복된다
            if (IsDead || amount <= 0)
            {
                return;
            }

            Current = Mathf.Max(0, Current - amount);
            Changed?.Invoke(Current, _maxHealth);

            if (IsDead)
            {
                Died?.Invoke();
            }
        }
    }
}
