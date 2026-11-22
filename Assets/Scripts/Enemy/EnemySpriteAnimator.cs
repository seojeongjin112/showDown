using UnityEngine;

namespace ArenaBreak.Enemy
{
    public class EnemySpriteAnimator : MonoBehaviour
    {
        private enum Action
        {
            Locomotion,
            Shoot,
            Reload
        }

        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite[] _walk;
        [SerializeField] private Sprite[] _shoot;
        [SerializeField] private Sprite[] _walkShoot;
        [SerializeField] private Sprite[] _reload;
        [SerializeField] private Sprite[] _walkReload;
        [SerializeField] private float _framesPerSecond = 12f;
        [SerializeField] private float _movingSpeedThreshold = 0.1f;

        private IEnemyAttack _attack;
        private Action _action;
        private float _actionElapsed;
        private Vector3 _lastPosition;
        private bool _isMoving;
        // 폭탄병처럼 장전 모션이 없는 적은 사격 뒤 바로 이동 상태로 돌아간다
        private bool _hasReload;

        private void Awake()
        {
            _attack = GetComponent<IEnemyAttack>();

            if (_attack == null || _renderer == null || IsEmpty(_walk) || IsEmpty(_shoot))
            {
                Debug.LogError("EnemySpriteAnimator: IEnemyAttack 또는 렌더러, 걷기/공격 프레임이 비어 있습니다", this);
                enabled = false;
                return;
            }

            // 서서 쏘는 적은 이동 전용 프레임이 없어도 되도록 정지 프레임으로 대체한다
            if (IsEmpty(_walkShoot))
            {
                _walkShoot = _shoot;
            }

            if (IsEmpty(_walkReload))
            {
                _walkReload = _reload;
            }

            _hasReload = !IsEmpty(_reload);
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;
            _attack.Attacked += OnAttacked;
        }

        private void OnDisable()
        {
            _attack.Attacked -= OnAttacked;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 delta = transform.position - _lastPosition;
            delta.y = 0f;
            _lastPosition = transform.position;
            _isMoving = delta.magnitude / deltaTime > _movingSpeedThreshold;

            _actionElapsed += deltaTime;
            AdvanceAction();
            _renderer.sprite = PickFrame();
        }

        private void OnAttacked(Transform target)
        {
            _action = Action.Shoot;
            _actionElapsed = 0f;
        }

        // 사격 직후 쿨다운 동안이 장전이므로 사격이 끝나면 곧바로 장전으로 잇는다
        private void AdvanceAction()
        {
            if (_action == Action.Locomotion)
            {
                return;
            }

            Sprite[] frames = _action == Action.Shoot ? _shoot : _reload;
            if (_actionElapsed * _framesPerSecond < frames.Length)
            {
                return;
            }

            _actionElapsed = 0f;
            _action = _action == Action.Shoot && _hasReload ? Action.Reload : Action.Locomotion;
        }

        private Sprite PickFrame()
        {
            Sprite[] frames;
            switch (_action)
            {
                case Action.Shoot:
                    frames = _isMoving ? _walkShoot : _shoot;
                    break;
                case Action.Reload:
                    frames = _isMoving ? _walkReload : _reload;
                    break;
                default:
                    // 정지 중에는 걷기 첫 프레임이 곧 대기 자세다
                    return _isMoving ? LoopFrame(_walk) : _walk[0];
            }

            // 이동 여부로 배열이 바뀌어도 진행 시간은 이어가고, 길이가 다르면 마지막 프레임에서 멈춘다
            int index = Mathf.Min((int)(_actionElapsed * _framesPerSecond), frames.Length - 1);
            return frames[index];
        }

        private Sprite LoopFrame(Sprite[] frames)
        {
            return frames[(int)(Time.time * _framesPerSecond) % frames.Length];
        }

        private static bool IsEmpty(Sprite[] frames)
        {
            return frames == null || frames.Length == 0;
        }
    }
}
