using System;

namespace ArenaBreak.Core
{
    // 타이틀에서 고른 난이도를 Game 씬까지 넘겨야 하는데, 씬 로드 사이에 살아남는 오브젝트를 두지 않으려고 static으로 들고 있다.
    // Game 씬을 타이틀 없이 바로 플레이하면 Current는 null이다
    public static class DifficultySelection
    {
        public static DifficultyData Current { get; private set; }

        public static event Action<DifficultyData> Changed;

        public static void Select(DifficultyData difficulty)
        {
            if (Current == difficulty)
            {
                return;
            }

            Current = difficulty;
            Changed?.Invoke(difficulty);
        }
    }
}
