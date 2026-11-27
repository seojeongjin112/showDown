using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    // 타이틀에서 고른 난이도를 화면 구석에 보여 준다. 판 도중에는 바뀌지 않으므로 시작할 때 한 번만 그린다
    public class DifficultyHUD : MonoBehaviour
    {
        [SerializeField] private Text _label;
        [Tooltip("{0}은 난이도 이름")]
        [SerializeField] private string _format = "난이도: {0}";
        [Tooltip("아무것도 고르지 않고 게임 씬을 바로 켰을 때의 이름")]
        [SerializeField] private string _defaultName = "보통";
        [SerializeField] private Color _easyColor = new Color(0.55f, 0.9f, 0.45f);
        [SerializeField] private Color _normalColor = new Color(1f, 0.85f, 0.5f);
        [SerializeField] private Color _hardColor = new Color(1f, 0.4f, 0.3f);

        private void Start()
        {
            if (_label == null)
            {
                Debug.LogError("DifficultyHUD: 글자 참조가 비어 있습니다", this);
                return;
            }

            DifficultyData current = Difficulty.Current;
            _label.text = string.Format(_format, current != null ? current.DisplayName : _defaultName);

            // 이름 대신 받는 피해 배율로 색을 고른다. 이름을 바꿔도 색이 맞게 따라간다
            float damage = Difficulty.DamageTaken;
            _label.color = damage < 1f ? _easyColor : damage > 1f ? _hardColor : _normalColor;
        }
    }
}
