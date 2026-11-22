using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class DamageFlashHUD : MonoBehaviour
    {
        [SerializeField] private Health _playerHealth;
        [SerializeField] private Image _flash;
        [SerializeField] private float _peakAlpha = 0.85f;
        [SerializeField] private float _fadeSeconds = 0.45f;

        private float _alpha;
        private int _lastHealth;

        private void Awake()
        {
            if (_playerHealth == null || _flash == null)
            {
                Debug.LogError("DamageFlashHUD: Health 또는 Image 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            // 화면 전체를 덮는 이미지가 버튼 클릭을 가로채지 않게 한다
            _flash.raycastTarget = false;
            SetAlpha(0f);
        }

        private void OnEnable()
        {
            _lastHealth = _playerHealth.Current;
            _playerHealth.Changed += OnHealthChanged;
        }

        private void OnDisable()
        {
            _playerHealth.Changed -= OnHealthChanged;
        }

        private void Update()
        {
            if (_alpha <= 0f)
            {
                return;
            }

            _alpha = Mathf.Max(0f, _alpha - _peakAlpha / _fadeSeconds * Time.deltaTime);
            SetAlpha(_alpha);
        }

        // 회복과 최대 체력 증가도 Changed로 오므로 체력이 줄어든 경우만 번쩍인다
        private void OnHealthChanged(int current, int max)
        {
            if (current < _lastHealth)
            {
                _alpha = _peakAlpha;
                SetAlpha(_alpha);
            }

            _lastHealth = current;
        }

        private void SetAlpha(float alpha)
        {
            Color color = _flash.color;
            color.a = alpha;
            _flash.color = color;
        }
    }
}
