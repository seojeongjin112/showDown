using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Core
{
    public class StoreInteractable : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string InteractActionName = "Interact";

        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("References")]
        [SerializeField] private Transform _player;

        [Header("Range")]
        // 건물 피벗이 아니라 문 앞 기준이므로, 씬에서 눈으로 보며 맞춘다
        [SerializeField] private float _interactRange = 3f;

        private InputAction _interactAction;
        private float _sqrRange;
        private bool _isInRange;

        public bool IsInRange => _isInRange;

        // 안내 문구는 범위가 바뀔 때만 갱신하면 되므로 프레임마다 알리지 않는다
        public event Action<bool> RangeChanged;
        public event Action Interacted;

        private void Awake()
        {
            if (_inputActions == null || _player == null)
            {
                Debug.LogError("StoreInteractable: 입력 에셋과 플레이어 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            InputActionMap map = _inputActions.FindActionMap(ActionMapName, true);
            _interactAction = map.FindAction(InteractActionName, true);
            _sqrRange = _interactRange * _interactRange;
        }

        private void OnEnable()
        {
            // 맵 전체가 아니라 액션만 켠다. 다른 컴포넌트가 같은 맵의 액션을 쓰기 때문이다
            _interactAction.Enable();
        }

        private void OnDisable()
        {
            if (_interactAction == null)
            {
                return;
            }

            _interactAction.Disable();
            SetInRange(false);
        }

        private void Update()
        {
            // 높이 차이는 무시한다. 건물 지붕 기준 피벗 때문에 수직 거리가 범위를 깎아먹지 않게 하려는 것이다
            Vector3 offset = _player.position - transform.position;
            offset.y = 0f;
            SetInRange(offset.sqrMagnitude <= _sqrRange);

            if (_isInRange && _interactAction.WasPressedThisFrame())
            {
                Interacted?.Invoke();
            }
        }

        private void SetInRange(bool value)
        {
            if (_isInRange == value)
            {
                return;
            }

            _isInRange = value;
            RangeChanged?.Invoke(value);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactRange);
        }
    }
}
