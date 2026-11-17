using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    public class CameraFollow : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string LookActionName = "Look";

        [Header("Target")]
        [SerializeField] private Transform _target;
        // 발이 아니라 몸통이 화면 중앙에 오도록 올리는 높이
        [SerializeField] private float _focusHeight = 1.2f;
        [SerializeField] private float _distance = 9f;
        [SerializeField] private float _smoothTime = 0.12f;
        // 0이면 캐릭터가 화면 중앙에 온다. 카메라를 오른쪽·위로 밀면 캐릭터가 왼쪽·아래로 비켜 선다
        [SerializeField] private Vector2 _shoulderOffset = Vector2.zero;

        [Header("Look")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private float _lookSensitivity = 0.1f;
        [SerializeField] private float _startPitch = 12f;
        [SerializeField] private float _minPitch = 5f;
        [SerializeField] private float _maxPitch = 30f;
        // 커서 잠금 첫 프레임에 delta가 크게 튀므로 프레임당 상한을 둔다
        [SerializeField] private float _maxLookDeltaPerFrame = 50f;
        [SerializeField] private bool _lockCursor = true;

        private InputAction _lookAction;
        private float _yaw;
        private float _pitch;
        private Vector3 _focusPoint;
        private Vector3 _velocity;

        private void Awake()
        {
            InputActionMap map = _inputActions.FindActionMap(ActionMapName, true);
            _lookAction = map.FindAction(LookActionName, true);
        }

        private void OnEnable()
        {
            // 맵 전체가 아니라 액션만 켠다. 다른 컴포넌트가 같은 맵의 액션을 쓰기 때문이다
            _lookAction.Enable();

            if (_lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            _lookAction.Disable();

            if (_lockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void Start()
        {
            _yaw = transform.eulerAngles.y;
            _pitch = Mathf.Clamp(_startPitch, _minPitch, _maxPitch);

            if (_target != null)
            {
                _focusPoint = _target.position + Vector3.up * _focusHeight;
            }
        }

        // 대상 이동이 Update에서 끝난 뒤에 따라가야 떨림이 없다
        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector2 look = Vector2.ClampMagnitude(_lookAction.ReadValue<Vector2>(), _maxLookDeltaPerFrame);
            _yaw += look.x * _lookSensitivity;
            _pitch = Mathf.Clamp(_pitch - look.y * _lookSensitivity, _minPitch, _maxPitch);

            // 회전은 입력에 바로 반응해야 하므로 위치만 부드럽게 따라간다
            Vector3 desiredFocus = _target.position + Vector3.up * _focusHeight;
            _focusPoint = Vector3.SmoothDamp(_focusPoint, desiredFocus, ref _velocity, _smoothTime);

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            // 어깨 오프셋은 카메라 로컬 축으로 더해야 회전해도 캐릭터가 항상 같은 화면 위치에 남는다
            Vector3 localOffset = new Vector3(_shoulderOffset.x, _shoulderOffset.y, -_distance);
            transform.SetPositionAndRotation(_focusPoint + rotation * localOffset, rotation);
        }
    }
}
