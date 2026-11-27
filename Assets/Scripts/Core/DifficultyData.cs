using UnityEngine;

namespace ArenaBreak.Core
{
    // 선택창이 보여 줄 난이도 하나. 난이도가 게임에 주는 효과는 난이도 담당 쪽에서 이 에셋을 기준으로 붙인다
    [CreateAssetMenu(fileName = "Difficulty", menuName = "ArenaBreak/Difficulty Data")]
    public class DifficultyData : ScriptableObject
    {
        [SerializeField] private string _displayName = "보통";
        [SerializeField, TextArea] private string _description;

        public string DisplayName => _displayName;
        public string Description => _description;
    }
}
