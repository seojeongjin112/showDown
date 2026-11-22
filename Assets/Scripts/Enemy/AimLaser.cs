using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 표시만 담당한다. 언제 켜고 끌지는 공격 컴포넌트가 정한다
    [RequireComponent(typeof(LineRenderer))]
    public class AimLaser : MonoBehaviour
    {
        // 투명에 가까운 색에서 진한 색으로 변해야 "곧 발사"가 읽힌다
        [SerializeField] private Color _startColor = new Color(1f, 0.2f, 0.2f, 0.15f);
        [SerializeField] private Color _endColor = new Color(1f, 0.05f, 0.05f, 1f);
        [SerializeField] private float _width = 0.05f;

        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.useWorldSpace = true;
            _line.startWidth = _width;
            _line.endWidth = _width;
            _line.enabled = false;
        }

        public void Show(Vector3 from, Vector3 to, float progress)
        {
            Color color = Color.Lerp(_startColor, _endColor, Mathf.Clamp01(progress));
            _line.startColor = color;
            _line.endColor = color;
            _line.SetPosition(0, from);
            _line.SetPosition(1, to);
            _line.enabled = true;
        }

        public void Hide()
        {
            _line.enabled = false;
        }
    }
}
