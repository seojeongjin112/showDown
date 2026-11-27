using UnityEngine;

namespace ArenaBreak.Core
{
    // 타이틀에서 고른 난이도를 게임 씬까지 들고 간다. 씬을 바로 실행해 아무것도 고르지 않았으면 모든 값이 보통(배율 1)이다
    public static class Difficulty
    {
        public static DifficultyData Current { get; set; }

        public static int PlayerMaxHealth => Current != null ? Current.PlayerMaxHealth : 0;
        public static float DamageTaken => Current != null ? Current.DamageTakenMultiplier : 1f;
        public static float HealDrop => Current != null ? Current.HealDropMultiplier : 1f;
        public static float EnemyHealth => Current != null ? Current.EnemyHealthMultiplier : 1f;
        public static float EnemyCount => Current != null ? Current.EnemyCountMultiplier : 1f;
        public static float MaxAlive => Current != null ? Current.MaxAliveMultiplier : 1f;
        public static float KillGoal => Current != null ? Current.KillGoalMultiplier : 1f;
        public static float BossHealth => Current != null ? Current.BossHealthMultiplier : 1f;
        public static float BossPatternInterval => Current != null ? Current.BossPatternIntervalMultiplier : 1f;

        // 수를 배율로 바꾸되 원래 1 이상이면 0이 되지 않게 한다
        public static int Scale(int value, float multiplier)
        {
            if (value <= 0)
            {
                return value;
            }

            return Mathf.Max(1, Mathf.RoundToInt(value * multiplier));
        }

        // 피해는 정수라서 곱한 뒤의 소수점은 그 확률로 올림한다. 쉬움에서 피해 1이 늘 1로 남지 않게 하되, 0으로 사라지지는 않게 한다
        public static int ScaleDamageTaken(int amount)
        {
            float multiplier = DamageTaken;
            if (amount <= 0 || Mathf.Approximately(multiplier, 1f))
            {
                return amount;
            }

            float scaled = amount * multiplier;
            int whole = Mathf.FloorToInt(scaled);
            int result = Random.value < scaled - whole ? whole + 1 : whole;
            return Mathf.Max(1, result);
        }
    }
}
