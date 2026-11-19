using ArenaBreak.Core;
using ArenaBreak.Player;
using UnityEngine;

namespace ArenaBreak.UI
{
    public class StorePanel : MonoBehaviour
    {
        [SerializeField] private StoreInteractable _interactable;
        [SerializeField] private CameraFollow _cameraFollow;
        [SerializeField] private GameObject _panelRoot;
        // 커서가 풀린 동안 화면 중앙에 조준점이 남으면 마우스 포인터와 겹쳐 보인다
        [SerializeField] private GameObject[] _hideWhileOpen;

        public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

        private void Awake()
        {
            if (_interactable == null || _cameraFollow == null || _panelRoot == null)
            {
                Debug.LogError("StorePanel: 상호작용 대상, 카메라, 패널 오브젝트 참조가 비어 있습니다", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _panelRoot.SetActive(false);
            _interactable.Interacted += OnInteracted;
            _interactable.RangeChanged += OnRangeChanged;
        }

        private void OnDisable()
        {
            if (_interactable == null)
            {
                return;
            }

            _interactable.Interacted -= OnInteracted;
            _interactable.RangeChanged -= OnRangeChanged;

            // 열린 채로 꺼지면 커서가 풀린 채 카메라가 멈춰 있게 된다
            if (IsOpen)
            {
                SetOpen(false);
            }
        }

        private void OnInteracted()
        {
            SetOpen(!IsOpen);
        }

        // 게임이 계속 진행되므로, 걸어 나가면 열린 창이 화면에 남지 않게 닫는다
        private void OnRangeChanged(bool inRange)
        {
            if (!inRange && IsOpen)
            {
                SetOpen(false);
            }
        }

        private void SetOpen(bool open)
        {
            _panelRoot.SetActive(open);
            _cameraFollow.SetLookSuspended(open);

            foreach (GameObject target in _hideWhileOpen)
            {
                if (target != null)
                {
                    target.SetActive(!open);
                }
            }
        }
    }
}
