using System;
using UnityEngine;

namespace ArenaBreak.Core
{
    public class Experience : MonoBehaviour
    {
        [Tooltip("1레벨에서 2레벨이 되는 데 필요한 경험치")]
        [SerializeField] private int _firstRequirement = 5;
        [Tooltip("레벨이 오를 때마다 필요 경험치가 늘어나는 양")]
        [SerializeField] private int _requirementGrowth = 3;

        public int Level { get; private set; } = 1;
        public int Current { get; private set; }
        public int Required => _firstRequirement + _requirementGrowth * (Level - 1);

        // HUD가 폴링하지 않고 구독해서 갱신하도록 레벨, 현재, 필요 경험치를 함께 넘긴다
        public event Action<int, int, int> Changed;
        public event Action<int> LeveledUp;

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Current += amount;

            // 한 번에 큰 경험치를 얻어도 여러 레벨이 한꺼번에 오를 수 있다
            while (Current >= Required)
            {
                Current -= Required;
                Level++;
                LeveledUp?.Invoke(Level);
            }

            Changed?.Invoke(Level, Current, Required);
        }

        // 필요 경험치가 0 이하면 위 반복이 끝나지 않는다
        private void OnValidate()
        {
            _firstRequirement = Mathf.Max(1, _firstRequirement);
            _requirementGrowth = Mathf.Max(0, _requirementGrowth);
        }
    }
}
