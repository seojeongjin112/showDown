using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(ViewmodelAnchor))]
    public class ViewmodelHitKick : MonoBehaviour
    {
        [SerializeField] private Health _playerHealth;
        // 팔 높이에 대한 비율. 맞은 순간 아래로 크게 밀려났다가 돌아온다
        [SerializeField] private Vector2 _kickOffset = new Vector2(0.04f, -0.12f);
        [SerializeField] private float _recoverSpeed = 10f;

        private ViewmodelAnchor _anchor;
        private Vector2 _current;
        private int _lastHealth;

        private void Awake()
        {
            _anchor = GetComponent<ViewmodelAnchor>();

            if (_playerHealth == null)
            {
                _playerHealth = GetComponentInParent<Health>();
            }

            if (_playerHealth == null)
            {
                Debug.LogError("ViewmodelHitKick: 플레이어 Health 참조를 찾지 못했습니다", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _lastHealth = _playerHealth.Current;
            _playerHealth.Changed += OnHealthChanged;
        }

        private void OnDisable()
        {
            _playerHealth.Changed -= OnHealthChanged;
            _current = Vector2.zero;
            _anchor.ImpactOffsetFraction = Vector2.zero;
        }

        private void Update()
        {
            // 회복·최대 체력 증가도 Changed로 오므로, 줄어든 경우만 반동으로 취급한다
            _current = Vector2.Lerp(_current, Vector2.zero, _recoverSpeed * Time.deltaTime);
            _anchor.ImpactOffsetFraction = _current;
        }

        private void OnHealthChanged(int current, int max)
        {
            if (current < _lastHealth)
            {
                _current = _kickOffset;
            }

            _lastHealth = current;
        }
    }
}
