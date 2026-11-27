using System;
using UnityEngine;

namespace ArenaBreak.Core
{
    // 팔 오브젝트가 꺼져 있는 동안에도 탄약을 사고 읽을 수 있도록 MonoBehaviour가 아닌 일반 클래스로 둔다
    public class WeaponAmmo
    {
        private readonly WeaponData _data;
        // 레벨업 카드로 예비 탄약 상한이 늘어난 배수
        private float _capMultiplier = 1f;

        public WeaponAmmo(WeaponData data)
        {
            _data = data;
            Magazine = data.MagazineSize;
            Reserve = Mathf.Min(data.StartReserve, data.MaxReserve);
        }

        public int Magazine { get; private set; }
        public int Reserve { get; private set; }

        public bool CanFire => Magazine > 0;
        public bool CanReload => Magazine < _data.MagazineSize && Reserve > 0;
        public int MaxReserve => Mathf.RoundToInt(_data.MaxReserve * _capMultiplier);
        public bool CanAddReserve => Reserve < MaxReserve;

        public void RaiseCap(float amount)
        {
            _capMultiplier += amount;
            Changed?.Invoke(Magazine, Reserve);
        }

        // HUD가 폴링하지 않고 구독해서 갱신하도록 탄창과 예비 탄약을 함께 넘긴다
        public event Action<int, int> Changed;

        public void Consume()
        {
            if (Magazine <= 0)
            {
                return;
            }

            Magazine--;
            Changed?.Invoke(Magazine, Reserve);
        }

        public void Reload()
        {
            int moved = Mathf.Min(_data.MagazineSize - Magazine, Reserve);
            if (moved <= 0)
            {
                return;
            }

            Magazine += moved;
            Reserve -= moved;
            Changed?.Invoke(Magazine, Reserve);
        }

        public void AddReserve(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Reserve = Mathf.Min(MaxReserve, Reserve + amount);
            Changed?.Invoke(Magazine, Reserve);
        }
    }
}
