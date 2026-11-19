using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class StoreMarkerView : MonoBehaviour
    {
        [SerializeField] private StoreInteractable _target;
        // 글자든 스프라이트든 색만 바꾸면 되므로 Graphic으로 받아 아이콘 교체에 묶이지 않게 한다
        [SerializeField] private Graphic _graphic;

        [Header("Bob")]
        [SerializeField] private float _bobHeight = 0.25f;
        [SerializeField] private float _bobSpeed = 2f;

        [Header("Highlight")]
        [SerializeField] private Color _idleColor = new Color(1f, 0.85f, 0.3f, 0.8f);
        [SerializeField] private Color _highlightColor = new Color(1f, 1f, 0.6f, 1f);
        [SerializeField] private float _highlightScale = 1.35f;
        [SerializeField] private float _blendSpeed = 8f;

        private Vector3 _baseLocalPosition;
        private Vector3 _baseLocalScale;
        private bool _isHighlighted;

        private void Awake()
        {
            if (_target == null || _graphic == null)
            {
                Debug.LogError("StoreMarkerView: 대상과 그래픽 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            _baseLocalPosition = transform.localPosition;
            _baseLocalScale = transform.localScale;
        }

        private void OnEnable()
        {
            _isHighlighted = _target.IsInRange;
            _target.RangeChanged += OnRangeChanged;
        }

        private void OnDisable()
        {
            if (_target != null)
            {
                _target.RangeChanged -= OnRangeChanged;
            }
        }

        private void OnRangeChanged(bool inRange)
        {
            _isHighlighted = inRange;
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
            transform.localPosition = _baseLocalPosition + Vector3.up * bob;

            // 상태가 바뀌는 순간 튀지 않도록 매 프레임 목표값으로 부드럽게 따라간다
            float blend = 1f - Mathf.Exp(-_blendSpeed * Time.deltaTime);
            Vector3 targetScale = _baseLocalScale * (_isHighlighted ? _highlightScale : 1f);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, blend);
            _graphic.color = Color.Lerp(_graphic.color, _isHighlighted ? _highlightColor : _idleColor, blend);
        }
    }
}
