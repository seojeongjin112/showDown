using System;
using UnityEngine;

namespace ArenaBreak.Core
{
    public class CoinWallet : MonoBehaviour
    {
        public int Count { get; private set; }

        // HUD가 폴링하지 않고 구독해서 갱신하도록 현재 개수를 넘긴다
        public event Action<int> Changed;

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Count += amount;
            Changed?.Invoke(Count);
        }

        // 부족하면 아무것도 깎지 않고 실패를 알려야 상점이 물건만 주고 값은 안 받는 일이 없다
        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > Count)
            {
                return false;
            }

            Count -= amount;
            Changed?.Invoke(Count);
            return true;
        }
    }
}
