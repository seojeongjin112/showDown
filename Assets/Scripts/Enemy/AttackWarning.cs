using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 근접 적이 공격을 준비하는 동안 머리 위에 느낌표를 띄워 곧 맞는다는 걸 알린다.
    // 예고가 끝나 갈수록 노랑에서 빨강으로 바뀌고 커지고 떨려서 피할 타이밍이 읽힌다
    [RequireComponent(typeof(MeleeAttack))]
    public class AttackWarning : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _icon;
        [Tooltip("적 그림 윗면에서 이만큼 위에 띄운다")]
        [SerializeField] private float _heightAbove = 0.35f;
        [SerializeField] private Color _earlyColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color _lateColor = new Color(1f, 0.15f, 0.1f, 1f);
        [SerializeField] private float _startScale = 0.9f;
        [SerializeField] private float _endScale = 1.5f;
        [Tooltip("예고 끝 무렵에 좌우로 떠는 폭(m)")]
        [SerializeField] private float _shake = 0.06f;
        [SerializeField] private float _shakeHz = 30f;

        private MeleeAttack _attack;
        private Vector3 _restLocalPosition;

        private void Awake()
        {
            _attack = GetComponent<MeleeAttack>();

            if (_icon == null)
            {
                Debug.LogError("AttackWarning: 아이콘 렌더러 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            // 적 그림 윗면 높이는 생성 시점 그림 경계에서 한 번만 잰다
            SpriteRenderer body = null;
            foreach (SpriteRenderer candidate in GetComponentsInChildren<SpriteRenderer>())
            {
                if (candidate != _icon)
                {
                    body = candidate;
                    break;
                }
            }

            float top = body != null ? body.bounds.max.y - transform.position.y : 2f;
            _restLocalPosition = new Vector3(0f, top + _heightAbove, 0f);
            _icon.transform.localPosition = _restLocalPosition;
            _icon.enabled = false;
        }

        private void OnEnable()
        {
            _attack.WindupChanged += OnWindupChanged;
        }

        private void OnDisable()
        {
            _attack.WindupChanged -= OnWindupChanged;

            if (_icon != null)
            {
                _icon.enabled = false;
            }
        }

        // 진행도가 0 미만이면 예고가 끝났다는 뜻이다
        private void OnWindupChanged(float progress)
        {
            if (progress < 0f)
            {
                _icon.enabled = false;
                return;
            }

            _icon.enabled = true;
            _icon.color = Color.Lerp(_earlyColor, _lateColor, progress);

            float scale = Mathf.Lerp(_startScale, _endScale, progress);
            _icon.transform.localScale = new Vector3(scale, scale, 1f);

            // 끝날 무렵에만 떨려서 처음에는 차분하다가 급해지는 느낌을 준다
            float shake = progress > 0.6f ? Mathf.Sin(Time.time * _shakeHz * Mathf.PI * 2f) * _shake * progress : 0f;
            _icon.transform.localPosition = _restLocalPosition + new Vector3(shake, 0f, 0f);
        }
    }
}
