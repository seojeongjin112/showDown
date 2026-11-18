using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    // Billboard와 OcclusionFader가 이 프레임에 확정된 카메라 위치·회전을 읽어야 하므로 가장 먼저 실행한다
    [DefaultExecutionOrder(-100)]
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
        // 시작 방향 기준 좌우 한계(도). 0 이하면 제한 없음. 적이 뒤에서 올 수 있어 기본은 360도 회전이다
        [SerializeField] private float _yawLimit = 0f;
        [SerializeField] private float _startPitch = 12f;
        [SerializeField] private float _minPitch = 5f;
        [SerializeField] private float _maxPitch = 30f;
        // 커서 잠금 첫 프레임에 delta가 크게 튀므로 프레임당 상한을 둔다
        [SerializeField] private float _maxLookDeltaPerFrame = 50f;
        [SerializeField] private bool _lockCursor = true;

        private InputAction _lookAction;
        private float _yaw;
        private float _centerYaw;
        private float _pitch;
        private Vector3 _focusPoint;
        private Vector3 _velocity;

        private void Awake()
        {
            if (_inputActions == null || _target == null)
            {
                Debug.LogError("CameraFollow: 입력 에셋 또는 추적 대상 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

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
            if (_lookAction == null)
            {
                return;
            }

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
            _centerYaw = _yaw;
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

            Vector2 look = ReadLook();
            _yaw += look.x * _lookSensitivity;
            if (_yawLimit > 0f)
            {
                _yaw = Mathf.Clamp(_yaw, _centerYaw - _yawLimit, _centerYaw + _yawLimit);
            }

            _pitch = Mathf.Clamp(_pitch - look.y * _lookSensitivity, _minPitch, _maxPitch);

            // 회전은 입력에 바로 반응해야 하므로 위치만 부드럽게 따라간다
            Vector3 desiredFocus = _target.position + Vector3.up * _focusHeight;
            _focusPoint = Vector3.SmoothDamp(_focusPoint, desiredFocus, ref _velocity, _smoothTime);

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            // 어깨 오프셋은 카메라 로컬 축으로 더해야 회전해도 캐릭터가 항상 같은 화면 위치에 남는다
            Vector3 localOffset = new Vector3(_shoulderOffset.x, _shoulderOffset.y, -_distance);
            transform.SetPositionAndRotation(_focusPoint + rotation * localOffset, rotation);
        }

        // Esc로 커서가 풀린 동안에도 포인터 이동량은 계속 들어오므로, 잠겨 있을 때만 카메라를 돌린다.
        // 풀린 뒤에는 왼쪽 클릭으로 다시 잠가야 카메라 조작을 되찾을 수 있다
        private Vector2 ReadLook()
        {
            if (_lockCursor && Cursor.lockState != CursorLockMode.Locked)
            {
                Mouse mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                return Vector2.zero;
            }

            return Vector2.ClampMagnitude(_lookAction.ReadValue<Vector2>(), _maxLookDeltaPerFrame);
        }
    }
}
