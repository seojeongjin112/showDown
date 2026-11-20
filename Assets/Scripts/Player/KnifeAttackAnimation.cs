using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class KnifeAttackAnimation : MonoBehaviour
    {
        // 입력 에셋의 Player 맵에는 우클릭 액션이 없어서 여기서 직접 바인딩한다
        private const string AttackBinding = "<Mouse>/rightButton";

        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _framesPerSecond = 18f;

        private SpriteRenderer _renderer;
        private InputAction _attackAction;
        private Sprite _idleSprite;
        private float _elapsed;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();

            if (_frames == null || _frames.Length == 0)
            {
                Debug.LogError("KnifeAttackAnimation: 공격 프레임이 비어 있습니다", this);
                enabled = false;
                return;
            }

            // 공격이 끝나면 되돌아갈 정지 모습은 씬에 놓인 스프라이트를 그대로 쓴다
            _idleSprite = _renderer.sprite;
            _attackAction = new InputAction("KnifeAttack", InputActionType.Button, AttackBinding);
        }

        private void OnEnable()
        {
            _attackAction.Enable();
        }

        private void OnDisable()
        {
            if (_attackAction == null)
            {
                return;
            }

            _attackAction.Disable();

            // 재생 도중에 꺼지면 공격 자세로 굳어 있지 않도록 되돌린다
            if (_isPlaying)
            {
                _isPlaying = false;
                _renderer.sprite = _idleSprite;
            }
        }

        private void OnDestroy()
        {
            _attackAction?.Dispose();
        }

        private void Update()
        {
            if (!_isPlaying)
            {
                // 커서가 풀린 동안의 클릭은 시점 재잠금용이므로 공격으로 쓰지 않는다
                if (_attackAction.WasPressedThisFrame() && Cursor.lockState == CursorLockMode.Locked)
                {
                    _isPlaying = true;
                    _elapsed = 0f;
                    _renderer.sprite = _frames[0];
                }

                return;
            }

            _elapsed += Time.deltaTime;
            int frameIndex = (int)(_elapsed * _framesPerSecond);

            if (frameIndex >= _frames.Length)
            {
                _isPlaying = false;
                _renderer.sprite = _idleSprite;
                return;
            }

            _renderer.sprite = _frames[frameIndex];
        }
    }
}
