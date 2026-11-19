using LucasoftArts.InfiniteCharacterForge.WildWest;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController25D : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string MoveActionName = "Move";
        private const string RollActionName = "Roll";
        private const float MinMoveInput = 0.01f;

        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("References")]
        [SerializeField] private Camera _camera;
        [SerializeField] private PixelCharacterAnimator _animator;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 3f;

        [Header("Roll")]
        [SerializeField] private float _rollDistance = 4f;
        [SerializeField] private float _rollDuration = 0.4f;
        // 시작 시점부터 센다. 지속 시간보다 길어야 연속 구르기 사이에 틈이 생긴다
        [SerializeField] private float _rollCooldown = 0.6f;

        [Header("Facing")]
        // 화면 정면(위·아래) 이동에서는 좌우가 깜빡이지 않도록 한다
        [SerializeField] private float _facingDeadZone = 0.2f;

        private CharacterController _controller;
        private SpriteRenderer _renderer;
        private InputAction _moveAction;
        private InputAction _rollAction;

        private Vector3 _aimDirection = Vector3.forward;
        private Vector3 _rollDirection;
        // 가만히 서서 구르기를 누를 때 방향이 없으므로 마지막 이동 방향을 기억해 둔다
        private Vector3 _lastMoveDirection;
        private bool _isRolling;
        private float _rollElapsed;
        private float _nextRollTime;
        private float _rollClipDuration;

        public bool IsRolling => _isRolling;
        // 구르는 동안은 피격을 무시해야 하므로 Health 쪽에서 이 값을 확인한다
        public bool IsInvulnerable => _isRolling;
        public Vector3 AimDirection => _aimDirection;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_animator == null)
            {
                _animator = GetComponentInChildren<PixelCharacterAnimator>();
            }

            if (_inputActions == null || _camera == null || _animator == null)
            {
                Debug.LogError("PlayerController25D: 입력 에셋, 카메라, 애니메이터 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            _renderer = _animator.GetComponent<SpriteRenderer>();

            InputActionMap map = _inputActions.FindActionMap(ActionMapName, true);
            _moveAction = map.FindAction(MoveActionName, true);
            _rollAction = map.FindAction(RollActionName, true);

            // 클립 길이는 고정값이라 한 번만 구해 두고, 구르기 지속 시간에 맞춰 재생 속도로 보정한다
            int rollFrames = ForgeAnimationInfo.FrameCount(ForgeAnimation.Roll);
            _rollClipDuration = (float)ForgeAnimationInfo.FrameStart(ForgeAnimation.Roll, rollFrames);
        }

        private void OnEnable()
        {
            // 맵 전체가 아니라 액션만 켠다. 다른 컴포넌트가 같은 맵의 액션을 쓰기 때문이다
            _moveAction.Enable();
            _rollAction.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _rollAction?.Disable();
            _isRolling = false;

            // 구르는 도중에 꺼지면 구르기용으로 올려 둔 재생 속도가 남아 다시 켰을 때 걷기가 빨라진다
            if (_animator != null)
            {
                _animator.speed = 1f;
            }
        }

        private void Update()
        {
            UpdateAim();

            if (_isRolling)
            {
                TickRoll();
                return;
            }

            if (_rollAction.WasPressedThisFrame() && Time.time >= _nextRollTime)
            {
                StartRoll();
                return;
            }

            Move();
        }

        // 커서가 잠기고 카메라가 마우스로 도므로, 조준은 카메라가 바라보는 수평 방향이다
        private void UpdateAim()
        {
            Vector3 forward = _camera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > MinMoveInput)
            {
                _aimDirection = forward.normalized;
            }
        }

        // 스프라이트는 카메라를 정면으로 보므로 좌우 반전은 카메라 기준 이동 방향으로 정한다.
        // SetFacing은 모든 클립을 다시 그리므로 부르지 않고 flipX만 바꾼다
        private void UpdateFacing(Vector3 worldDirection)
        {
            float side = Vector3.Dot(worldDirection.normalized, _camera.transform.right);
            if (Mathf.Abs(side) > _facingDeadZone)
            {
                _renderer.flipX = side < 0f;
            }
        }

        // 입력은 화면 기준이므로 카메라 yaw만큼 돌려서 월드 방향으로 바꾼다
        private Vector3 ReadMoveDirection()
        {
            Vector2 input = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);
            Quaternion yaw = Quaternion.Euler(0f, _camera.transform.eulerAngles.y, 0f);
            return yaw * new Vector3(input.x, 0f, input.y);
        }

        private void Move()
        {
            Vector3 direction = ReadMoveDirection();
            _controller.Move(direction * (_moveSpeed * Time.deltaTime));

            bool isMoving = direction.sqrMagnitude > MinMoveInput;
            if (isMoving)
            {
                _lastMoveDirection = direction.normalized;
                UpdateFacing(direction);
            }

            // 사격 클립이 끝나기 전에 걷기·대기로 덮어쓰면 총을 쏘는 모습이 한 프레임도 안 보인다
            if (_animator.CurrentAnimation == ForgeAnimation.Shoot)
            {
                return;
            }

            _animator.Play(isMoving ? ForgeAnimation.Walk : ForgeAnimation.Idle);
        }

        private void StartRoll()
        {
            _isRolling = true;
            _rollElapsed = 0f;
            _rollDirection = ResolveRollDirection();
            _nextRollTime = Time.time + _rollCooldown;
            UpdateFacing(_rollDirection);

            _animator.speed = _rollClipDuration / Mathf.Max(_rollDuration, MinMoveInput);
            _animator.Play(ForgeAnimation.Roll, true);
        }

        private Vector3 ResolveRollDirection()
        {
            Vector3 input = ReadMoveDirection();
            if (input.sqrMagnitude > MinMoveInput)
            {
                return input.normalized;
            }

            // 입력이 없으면 마지막 이동 방향, 그것도 없으면(시작 직후) 카메라 정면으로 구른다
            return _lastMoveDirection.sqrMagnitude > 0f ? _lastMoveDirection : _aimDirection;
        }

        private void TickRoll()
        {
            _rollElapsed += Time.deltaTime;

            float speed = _rollDistance / Mathf.Max(_rollDuration, MinMoveInput);
            _controller.Move(_rollDirection * (speed * Time.deltaTime));

            if (_rollElapsed >= _rollDuration)
            {
                EndRoll();
            }
        }

        private void EndRoll()
        {
            _isRolling = false;
            _animator.speed = 1f;
            _animator.Play(ForgeAnimation.Idle);
        }
    }
}
