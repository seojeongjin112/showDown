using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.UI
{
    public class InteractPromptView : MonoBehaviour
    {
        [SerializeField] private StoreInteractable _target;
        // 문구 오브젝트를 켜고 끌 뿐이라 텍스트 종류(TMP/uGUI)에 묶이지 않는다
        [SerializeField] private GameObject _promptRoot;

        private void Awake()
        {
            if (_target == null || _promptRoot == null)
            {
                Debug.LogError("InteractPromptView: 대상과 문구 오브젝트 참조가 비어 있습니다", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _promptRoot.SetActive(_target.IsInRange);
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
            _promptRoot.SetActive(inRange);
        }
    }
}
