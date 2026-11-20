using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class HealthBarHUD : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [Tooltip("왼쪽부터 체력 1칸씩. 개수가 최대 체력과 같아야 한다")]
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
            Sprite filled = current <= _lowThreshold ? _lowSprite : _fullSprite;

            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].enabled = i < current;
                _cells[i].sprite = filled;
            }
        }
    }
}
