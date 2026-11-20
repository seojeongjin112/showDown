using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonMovement : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string MoveActionName = "Move";

        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private float _moveSpeed = 4f;

        private CharacterController _controller;
        private InputAction _moveAction;

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
        }

        private void OnEnable()
        {
            // 맵 전체가 아니라 액션만 켠다. 다른 컴포넌트가 같은 맵의 액션을 쓰기 때문이다
            _moveAction.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
        }

        private void Update()
        {
            Vector2 input = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);

            // 몸이 시점 yaw를 따라 돌기 때문에 몸 기준 축으로 움직이면 바라보는 방향이 앞이 된다
            Vector3 direction = transform.right * input.x + transform.forward * input.y;

            // SimpleMove는 중력을 알아서 적용하고 속도를 초당 단위로 받는다
            _controller.SimpleMove(direction * _moveSpeed);
        }
    }
}
