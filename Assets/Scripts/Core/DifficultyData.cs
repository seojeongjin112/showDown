using UnityEngine;

namespace ArenaBreak.Core
{
    [CreateAssetMenu(fileName = "Difficulty", menuName = "ArenaBreak/Difficulty Data")]
    public class DifficultyData : ScriptableObject
    {
        [SerializeField] private string _displayName = "보통";
        [SerializeField, TextArea] private string _description;

        // 웨이브·조우 데이터를 난이도마다 복제하지 않으려고 배율만 들고 있다
        [Header("Multipliers")]
        [Tooltip("적이 생성될 때 최대 체력에 곱한다")]
        [SerializeField] private float _enemyHealthMultiplier = 1f;

        [Tooltip("플레이어가 받는 피해에 곱한다")]
        [SerializeField] private float _incomingDamageMultiplier = 1f;

        [Tooltip("플레이어 최대 체력에 곱한다")]
        [SerializeField] private float _playerHealthMultiplier = 1f;

        public string DisplayName => _displayName;
        public string Description => _description;
        public float EnemyHealthMultiplier => _enemyHealthMultiplier;
        public float IncomingDamageMultiplier => _incomingDamageMultiplier;
        public float PlayerHealthMultiplier => _playerHealthMultiplier;

        // 0이 되면 적이 바로 죽거나 플레이어가 무적이 되므로 하한을 둔다
        private const float MinMultiplier = 0.1f;

        private void OnValidate()
        {
            _enemyHealthMultiplier = Mathf.Max(MinMultiplier, _enemyHealthMultiplier);
            _incomingDamageMultiplier = Mathf.Max(MinMultiplier, _incomingDamageMultiplier);
            _playerHealthMultiplier = Mathf.Max(MinMultiplier, _playerHealthMultiplier);
        }
    }
}
