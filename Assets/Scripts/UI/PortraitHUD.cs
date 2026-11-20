using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class PortraitHUD : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Image _face;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _hurtSprite;
        [SerializeField] private Sprite _criticalSprite;
        // 최대 체력이 바뀌어도 단계가 유지되도록 비율로 둔다
        [SerializeField, Range(0f, 1f)] private float _hurtRatio = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _criticalRatio = 0.3f;

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
            float ratio = (float)current / max;

            if (ratio <= _criticalRatio)
            {
                _face.sprite = _criticalSprite;
            }
            else if (ratio <= _hurtRatio)
            {
                _face.sprite = _hurtSprite;
            }
            else
            {
                _face.sprite = _normalSprite;
            }
        }
    }
}
