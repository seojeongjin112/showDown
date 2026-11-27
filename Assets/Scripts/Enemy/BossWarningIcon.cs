using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 보스가 공격을 준비하는 동안 몸통 가운데에 경고 표시를 띄운다. 노랑에서 빨강으로 바뀌고 커지며 끝 무렵에 떨린다
    public class BossWarningIcon : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _icon;
        [Tooltip("보스 그림 가운데에서 이만큼 위(+)나 아래(-)에 띄운다")]
        [SerializeField] private float _heightOffset;
        [SerializeField] private Color _earlyColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color _lateColor = new Color(1f, 0.15f, 0.1f, 1f);
        [SerializeField] private float _startScale = 1.4f;
        [SerializeField] private float _endScale = 2.2f;
        [Tooltip("예고 끝 무렵에 좌우로 떠는 폭(m)")]
        [SerializeField] private float _shake = 0.15f;
        [SerializeField] private float _shakeHz = 30f;

        private Vector3 _restPosition;
        private float _startTime;
        private float _seconds;

        private void Awake()
        {
            if (_icon == null)
            {
                Debug.LogError("BossWarningIcon: 아이콘 렌더러 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            // 몸통 가운데 높이는 생성 시점 그림 경계에서 한 번만 잰다
            float middle = 2f;
            foreach (SpriteRenderer candidate in GetComponentsInChildren<SpriteRenderer>())
            {
                if (candidate != _icon && candidate.sprite != null)
                {
                    middle = candidate.bounds.center.y - transform.position.y;
                    break;
                }
            }

            _restPosition = new Vector3(0f, middle + _heightOffset, 0f);
            _icon.transform.localPosition = _restPosition;
            _icon.enabled = false;
        }

        public void Show(float seconds)
        {
            _startTime = Time.time;
            _seconds = seconds;
        }

        public void Hide()
        {
            _seconds = 0f;
            if (_icon != null)
            {
                _icon.enabled = false;
            }
        }

        private void Update()
        {
            if (_seconds <= 0f)
            {
                return;
            }

            float progress = (Time.time - _startTime) / _seconds;
            if (progress >= 1f)
            {
                Hide();
                return;
            }

            _icon.enabled = true;
            _icon.color = Color.Lerp(_earlyColor, _lateColor, progress);
            float scale = Mathf.Lerp(_startScale, _endScale, progress);
            _icon.transform.localScale = new Vector3(scale, scale, 1f);

            float shake = progress > 0.6f ? Mathf.Sin(Time.time * _shakeHz * Mathf.PI * 2f) * _shake * progress : 0f;
            _icon.transform.localPosition = _restPosition + new Vector3(shake, 0f, 0f);
        }
    }
}
