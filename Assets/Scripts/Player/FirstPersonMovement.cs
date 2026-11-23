using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonMovement : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string MoveActionName = "Move";
        private const string JumpActionName = "Jump";

        // 0으로 두면 매 프레임 isGrounded가 흔들려서 점프 입력을 놓치므로 바닥에 살짝 눌러 둔다
        private const float GroundedStickVelocity = -2f;

        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _jumpHeight = 1.2f;

        private CharacterController _controller;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private float _verticalVelocity;
        private float _speedMultiplier = 1f;

        // 얕은 물 같은 지형이 일시적으로 늦출 때 쓴다. 강화로 쌓이는 _moveSpeed와 섞이면 물을 벗어난 뒤 원래 속도로 돌릴 수 없어 따로 둔다
        public float SpeedMultiplier
        {
            get => _speedMultiplier;
            set => _speedMultiplier = Mathf.Max(0f, value);
        }

        public void AddMoveSpeed(float amount)
        {
            _moveSpeed += amount;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (_inputActions == null)
            {
                Debug.LogError("FirstPersonMovement: 입력 에셋 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            InputActionMap map = _inputActions.FindActionMap(ActionMapName, true);
            _moveAction = map.FindAction(MoveActionName, true);
            _jumpAction = map.FindAction(JumpActionName, true);
        }

        private void OnEnable()
        {
            // 맵 전체가 아니라 액션만 켠다. 다른 컴포넌트가 같은 맵의 액션을 쓰기 때문이다
            _moveAction.Enable();
            _jumpAction.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
        }

        private void Update()
        {
            Vector2 input = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);

            // 몸이 시점 yaw를 따라 돌기 때문에 몸 기준 축으로 움직이면 바라보는 방향이 앞이 된다
            Vector3 direction = transform.right * input.x + transform.forward * input.y;

            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = GroundedStickVelocity;
                }

                if (_jumpAction.WasPressedThisFrame())
                {
                    // 높이에서 초기 속도를 역산해야 중력을 바꿔도 점프 높이가 유지된다
                    _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * Physics.gravity.y);
                }
            }

            _verticalVelocity += Physics.gravity.y * Time.deltaTime;

            Vector3 velocity = direction * (_moveSpeed * _speedMultiplier);
            velocity.y = _verticalVelocity;

            // SimpleMove는 수직 속도를 받지 못해 점프할 수 없어서 Move로 직접 중력을 적용한다
            _controller.Move(velocity * Time.deltaTime);
        }
    }
}
