using System.Collections;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class ExperienceBarHUD : MonoBehaviour
    {
        [SerializeField] private Experience _experience;
        [Tooltip("왼쪽부터 차오르는 Filled 타입 이미지")]
        [SerializeField] private Image _fill;
        [Tooltip("경험치를 얻은 순간 채움 위에 잠깐 번쩍이는 Filled 타입 이미지")]
        [SerializeField] private Image _flash;
        [SerializeField] private Text _label;
        [SerializeField] private string _labelFormat = "Lv{0}";
        [SerializeField] private float _flashDuration = 0.25f;

        private Coroutine _flashRoutine;

        private void OnEnable()
        {
            if (_experience == null)
            {
                return;
            }

            _experience.Changed += OnChanged;
            _flash.enabled = false;
            Refresh(_experience.Level, _experience.Current, _experience.Required);
        }

        private void OnDisable()
        {
            if (_experience != null)
            {
                _experience.Changed -= OnChanged;
            }
        }

        private void OnChanged(int level, int current, int required)
        {
            Refresh(level, current, required);

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(Flash());
        }

        // Experience는 시작 시 Changed를 쏘지 않으므로 OnEnable에서 현재값으로 한 번 그린다
        private void Refresh(int level, int current, int required)
        {
            float ratio = (float)current / required;
            _fill.fillAmount = ratio;
            // 번쩍임이 빈 칸까지 덮지 않도록 채워진 만큼만 겹친다
            _flash.fillAmount = ratio;
            _label.text = string.Format(_labelFormat, level);
        }

        private IEnumerator Flash()
        {
            _flash.enabled = true;
            Color color = _flash.color;

            for (float elapsed = 0f; elapsed < _flashDuration; elapsed += Time.deltaTime)
            {
                color.a = 1f - elapsed / _flashDuration;
                _flash.color = color;
                yield return null;
            }

            _flash.enabled = false;
            _flashRoutine = null;
        }
    }
}
