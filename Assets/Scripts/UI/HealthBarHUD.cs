using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class HealthBarHUD : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [Tooltip("왼쪽부터 채워지는 칸. 최대 체력과 개수가 같지 않아도 비율로 그린다")]
        [SerializeField] private Image[] _cells;
        [SerializeField] private Sprite _fullSprite;
        [SerializeField] private Sprite _lowSprite;
        [SerializeField] private int _lowThreshold = 3;

        private void OnEnable()
        {
            if (_health == null)
            {
                return;
            }

            _health.Changed += Refresh;
            Refresh(_health.Current, _health.Max);
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Changed -= Refresh;
            }
        }

        // Health는 시작 시 Changed를 쏘지 않으므로 OnEnable에서 현재값으로 한 번 그린다
        private void Refresh(int current, int max)
        {
            // 최대 체력이 강화로 늘어도 칸 수는 그대로여서 비율로 채우고, 1칸 분량만 남아도 한 칸은 보이도록 올림한다
            int filledCells = Mathf.CeilToInt((float)current / max * _cells.Length);
            Sprite filled = filledCells <= _lowThreshold ? _lowSprite : _fullSprite;

            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].enabled = i < filledCells;
                _cells[i].sprite = filled;
            }
        }
    }
}
