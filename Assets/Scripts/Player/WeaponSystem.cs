using LucasoftArts.InfiniteCharacterForge.WildWest;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(PlayerController25D))]
    public class WeaponSystem : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string FireActionName = "Attack";

        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private bool _holdToFire = true;

        [Header("References")]
        [SerializeField] private PixelCharacterAnimator _animator;
        [SerializeField] private Bullet _bulletPrefab;
        [SerializeField] private Camera _camera;
        // 조준선이 화면 중앙에서 벗어나 있어도 그 위치로 쏘게 한다. 비워 두면 화면 중앙을 쓴다
        [SerializeField] private RectTransform _crosshair;

        [Header("Aim")]
        // 플레이어 자신과 아군 레이어는 빼 둔다
        [SerializeField] private LayerMask _aimLayers = ~0;
        // 아무것도 맞지 않을 때 조준점으로 삼는 거리
        [SerializeField] private float _aimMaxDistance = 50f;

        [Header("Fire")]
        [SerializeField] private int _damage = 10;
        // 시작 시점부터 센다. Shoot 클립보다 짧으면 애니메이션이 재시작되며 총알이 앞당겨 나간다
        [SerializeField] private float _fireInterval = 0.4f;

        [Header("Muzzle")]
        // 스프라이트 픽셀 좌표를 월드로 환산하는 대신, 총구를 플레이어 기준 오프셋으로 근사한다
        [SerializeField] private float _muzzleHeight = 1f;
        [SerializeField] private float _muzzleForward = 0.5f;

        [Header("Pool")]
        [SerializeField] private int _poolDefaultCapacity = 16;
        [SerializeField] private int _poolMaxSize = 64;

        private PlayerController25D _player;
        private InputAction _fireAction;
        private ObjectPool<Bullet> _pool;
        private float _nextFireTime;
        // ShotFired는 애니메이션 프레임에서 오므로, 우리가 요청한 사격일 때만 총알을 만든다
        private bool _shotPending;

        private void Awake()
        {
            _player = GetComponent<PlayerController25D>();

            if (_animator == null)
            {
                _animator = GetComponentInChildren<PixelCharacterAnimator>();
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_inputActions == null || _animator == null || _bulletPrefab == null || _camera == null)
            {
                Debug.LogError("WeaponSystem: 입력 에셋, 애니메이터, 총알 프리팹, 카메라 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            InputActionMap map = _inputActions.FindActionMap(ActionMapName, true);
            _fireAction = map.FindAction(FireActionName, true);

            _pool = new ObjectPool<Bullet>(
                CreateBullet,
                OnGetBullet,
                OnReleaseBullet,
                OnDestroyBullet,
                collectionCheck: false,
                _poolDefaultCapacity,
                _poolMaxSize);
        }

        private void OnEnable()
        {
            // 맵 전체가 아니라 액션만 켠다. 다른 컴포넌트가 같은 맵의 액션을 쓰기 때문이다
            _fireAction.Enable();
            _animator.ShotFired += OnShotFired;
        }

        private void OnDisable()
        {
            if (_fireAction == null)
            {
                return;
            }

            _fireAction.Disable();
            _animator.ShotFired -= OnShotFired;
            _shotPending = false;
        }

        private void Update()
        {
            if (_player.IsRolling || Time.time < _nextFireTime)
            {
                return;
            }

            bool wantsFire = _holdToFire ? _fireAction.IsPressed() : _fireAction.WasPressedThisFrame();
            if (!wantsFire)
            {
                return;
            }

            // 커서가 풀린 동안의 클릭은 카메라 재잠금용이므로 사격으로 쓰지 않는다
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            _nextFireTime = Time.time + _fireInterval;
            _shotPending = true;
            _animator.Play(ForgeAnimation.Shoot, true);
        }

        private void OnShotFired(int frame)
        {
            if (!_shotPending)
            {
                return;
            }

            _shotPending = false;

            Vector3 horizontal = _player.AimDirection;
            Vector3 muzzle = transform.position + Vector3.up * _muzzleHeight + horizontal * _muzzleForward;
            Vector3 direction = (FindAimPoint() - muzzle).normalized;

            Bullet bullet = _pool.Get();
            bullet.Initialize(muzzle, direction, _damage, ReleaseBullet);
        }

        // 총구가 카메라와 떨어져 있어 카메라 정면으로 쏘면 조준선과 어긋나므로, 조준선이 가리키는 월드 지점으로 향한다
        private Vector3 FindAimPoint()
        {
            Vector2 screenPoint = _crosshair != null
                ? RectTransformUtility.WorldToScreenPoint(null, _crosshair.position)
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Ray ray = _camera.ScreenPointToRay(screenPoint);

            // 카메라는 플레이어 뒤에 있어 레이가 자기 몸을 먼저 맞힌다. 플레이어 앞에서 시작해 건너뛴다
            float alongRay = Vector3.Dot(transform.position - ray.origin, ray.direction);
            Vector3 start = ray.origin + ray.direction * Mathf.Max(alongRay, 0f);

            if (Physics.Raycast(start, ray.direction, out RaycastHit hit, _aimMaxDistance, _aimLayers, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return start + ray.direction * _aimMaxDistance;
        }

        private void ReleaseBullet(Bullet bullet)
        {
            _pool.Release(bullet);
        }

        private Bullet CreateBullet()
        {
            return Instantiate(_bulletPrefab);
        }

        private static void OnGetBullet(Bullet bullet)
        {
            bullet.gameObject.SetActive(true);
        }

        private static void OnReleaseBullet(Bullet bullet)
        {
            bullet.gameObject.SetActive(false);
        }

        private static void OnDestroyBullet(Bullet bullet)
        {
            Destroy(bullet.gameObject);
        }
    }
}
