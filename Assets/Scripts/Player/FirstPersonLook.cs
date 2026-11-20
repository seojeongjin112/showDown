using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    // 좌우 회전은 몸에, 상하 회전은 카메라에 준다. 몸이 돌아야 이동 방향이 시점을 따라간다
    public class FirstPersonLook : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string LookActionName = "Look";

        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private float _lookSensitivity = 0.1f;
        [SerializeField] private float _minPitch = -80f;
        [SerializeField] private float _maxPitch = 80f;
        // 커서 잠금 첫 프레임에 delta가 크게 튀므로 프레임당 상한을 둔다
        [SerializeField] private float _maxLookDeltaPerFrame = 50f;

        private InputAction _lookAction;
        private float _pitch;

        private void Awake()
        {
            if (_inputActions == null || _cameraTransform == null)
            {
                Debug.LogError("FirstPersonLook: 입력 에셋과 카메라 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            InputActionMap map = _inputActions.FindActionMap(ActionMapName, true);
            _lookAction = map.FindAction(LookActionName, true);
        }

        private void OnEnable()
        {
            _lookAction.Enable();
            LockCursor();
        }

        private void OnDisable()
        {
            if (_lookAction == null)
            {
                return;
            }

            _lookAction.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            // Esc로 커서가 풀린 동안에는 시점을 돌리지 않고, 왼쪽 클릭으로 다시 잠근다
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Mouse mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    LockCursor();
                }

                return;
            }

            Vector2 look = Vector2.ClampMagnitude(_lookAction.ReadValue<Vector2>(), _maxLookDeltaPerFrame) * _lookSensitivity;

            transform.Rotate(Vector3.up, look.x, Space.World);

            _pitch = Mathf.Clamp(_pitch - look.y, _minPitch, _maxPitch);
            _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
