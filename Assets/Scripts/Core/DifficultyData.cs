using UnityEngine;

namespace ArenaBreak.Core
{
    // 난이도 한 단계의 값 묶음. 보통은 모든 배율이 1이고, 쉬움·어려움은 여기서 숫자만 바꿔 만든다
    [CreateAssetMenu(fileName = "Difficulty", menuName = "ArenaBreak/Difficulty")]
    public class DifficultyData : ScriptableObject
    {
        [SerializeField] private string _displayName = "보통";
        [TextArea(1, 3)]
        [SerializeField] private string _description;

        [Header("플레이어")]
        [Tooltip("시작 최대 체력. 0이면 플레이어 프리팹의 값을 그대로 쓴다")]
        [SerializeField] private int _playerMaxHealth;
        [Tooltip("플레이어가 받는 피해에 곱한다")]
        [SerializeField] private float _damageTakenMultiplier = 1f;
        [Tooltip("적이 체력 회복 아이템을 떨어뜨릴 확률에 곱한다")]
        [SerializeField] private float _healDropMultiplier = 1f;

        [Header("적")]
        [Tooltip("모든 적의 최대 체력에 곱한다")]
        [SerializeField] private float _enemyHealthMultiplier = 1f;
        [Tooltip("조우 구역과 웨이브가 내보내는 적 수에 곱한다")]
        [SerializeField] private float _enemyCountMultiplier = 1f;
        [Tooltip("조우 구역에서 동시에 살아 있을 수 있는 적 수에 곱한다. 클수록 한꺼번에 몰려온다")]
        [SerializeField] private float _maxAliveMultiplier = 1f;
        [Tooltip("해골 문을 여는 데 필요한 처치 수에 곱한다")]
        [SerializeField] private float _killGoalMultiplier = 1f;

        [Header("보스")]
        [Tooltip("보스 체력에 곱한다. 위의 적 체력 배율과 함께 곱해진다")]
        [SerializeField] private float _bossHealthMultiplier = 1f;
        [Tooltip("보스가 패턴 사이에 쉬는 시간에 곱한다. 작을수록 패턴이 잦다")]
        [SerializeField] private float _bossPatternIntervalMultiplier = 1f;

        public string DisplayName => _displayName;
        public string Description => _description;
        public int PlayerMaxHealth => _playerMaxHealth;
        public float DamageTakenMultiplier => _damageTakenMultiplier;
        public float HealDropMultiplier => _healDropMultiplier;
        public float EnemyHealthMultiplier => _enemyHealthMultiplier;
        public float EnemyCountMultiplier => _enemyCountMultiplier;
        public float MaxAliveMultiplier => _maxAliveMultiplier;
        public float KillGoalMultiplier => _killGoalMultiplier;
        public float BossHealthMultiplier => _bossHealthMultiplier;
        public float BossPatternIntervalMultiplier => _bossPatternIntervalMultiplier;
    }
}
