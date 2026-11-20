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
    }
}
