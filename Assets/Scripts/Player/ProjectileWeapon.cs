using System;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    // 활과 토마호크처럼 손을 떠나는 순간에 비행체를 내보내는 무기. 우클릭으로 쏘고 R로 장전한다
    [RequireComponent(typeof(SpriteRenderer))]
    public class ProjectileWeapon : MonoBehaviour, IAmmoWeapon
    {
        // 입력 에셋의 Player 맵에는 우클릭과 장전 액션이 없어서 여기서 직접 바인딩한다
        private const string FireBinding = "<Mouse>/rightButton";
        private const string ReloadBinding = "<Keyboard>/r";

        private enum State
        {
            Idle,
            Firing,
            Reloading
        }

        [SerializeField] private WeaponData _data;
        [SerializeField] private Camera _camera;
        [SerializeField] private PlayerProjectile _projectilePrefab;
        [Tooltip("발사 애니메이션에서 손을 떠나는 프레임 번호")]
        [SerializeField] private int _releaseFrame = 5;
        [SerializeField] private float _speed = 40f;
        [Tooltip("0이면 일직선, 1이면 중력 그대로. 화살은 작게, 도끼는 크게 둔다")]
        [SerializeField] private float _gravityScale = 0.3f;
        [SerializeField] private float _spawnDistance = 0.6f;
        // 눈높이에서 그대로 나가면 시선 가운데를 가려서 조금 아래에서 나간다
        [SerializeField] private float _spawnDownOffset = 0.12f;
        [Tooltip("쏜 뒤 탄창이 비면 R을 누르지 않아도 바로 다음 것을 꺼낸다")]
        [SerializeField] private bool _reloadAfterShot = true;

        private SpriteRenderer _renderer;
        private PlayerUpgrades _upgrades;
        private InputAction _fireAction;
        private InputAction _reloadAction;
        private Sprite _idleSprite;
        private WeaponAmmo _ammo;
        private State _state;
        private Sprite[] _frames;
        private float _elapsed;
        private bool _released;

        // 명중 표시(HUD)가 구현을 몰라도 구독할 수 있게 열어 둔다. 인자는 처치 여부
        public event Action<bool> Hit;

        public void Bind(WeaponAmmo ammo)
        {
            _ammo = ammo;
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _idleSprite = _renderer.sprite;
            _upgrades = GetComponentInParent<PlayerUpgrades>();

            if (_camera == null)
            {
                _camera = GetComponentInParent<Camera>();
            }

            if (_data == null || _camera == null || _projectilePrefab == null)
            {
                Debug.LogError("ProjectileWeapon: 무기 데이터, 카메라, 비행체 프리팹 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            _fireAction = new InputAction("ProjectileFire", InputActionType.Button, FireBinding);
            _reloadAction = new InputAction("ProjectileReload", InputActionType.Button, ReloadBinding);
        }

        private void OnEnable()
        {
            _fireAction.Enable();
            _reloadAction.Enable();
        }

        private void OnDisable()
        {
            if (_fireAction == null)
            {
                return;
            }

            _fireAction.Disable();
            _reloadAction.Disable();

            // 쏘는 도중에 무기를 바꾸면 아직 손을 떠나지 않았으므로 탄약은 그대로 두고 처음 자세로 되돌린다
            _state = State.Idle;
            _renderer.sprite = _idleSprite;
        }

        private void OnDestroy()
        {
            _fireAction?.Dispose();
            _reloadAction?.Dispose();
        }

        private void Update()
        {
            if (_state == State.Idle)
            {
                ReadInput();
                return;
            }

            Animate();
        }

        private void ReadInput()
        {
            // 커서가 풀린 동안의 클릭은 시점 재잠금용이므로 발사로 쓰지 않는다
            if (_ammo == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            if (_fireAction.WasPressedThisFrame())
            {
                TryFire();
            }
            else if (_reloadAction.WasPressedThisFrame())
            {
                TryReload();
            }
        }

        private void TryFire()
        {
            // 빈 탄창으로 쏘면 헛방 대신 바로 장전을 시작해 준다
            if (!_ammo.CanFire)
            {
                TryReload();
                return;
            }

            _released = false;
            Play(State.Firing, _data.FireFrames);
        }

        private void TryReload()
        {
            if (_ammo.CanReload)
            {
                Play(State.Reloading, _data.ReloadFrames);
            }
        }

        private void Play(State state, Sprite[] frames)
        {
            if (frames == null || frames.Length == 0)
            {
                // 프레임이 없으면 애니메이션 없이 바로 끝난 것으로 처리해 장전이 영영 안 끝나는 일이 없게 한다
                if (state == State.Reloading)
                {
                    _ammo.Reload();
                }

                return;
            }

            _state = state;
            _frames = frames;
            _elapsed = 0f;
            _renderer.sprite = frames[0];
        }

        private void Animate()
        {
            _elapsed += Time.deltaTime;
            float fps = _state == State.Reloading
                ? _data.ReloadFramesPerSecond * (_upgrades != null ? _upgrades.ReloadSpeedMultiplier : 1f)
                : _data.FramesPerSecond;
            int frameIndex = (int)(_elapsed * fps);

            if (_state == State.Firing && !_released && frameIndex >= _releaseFrame)
            {
                _released = true;
                Release();
            }

            if (frameIndex >= _frames.Length)
            {
                FinishAnimation();
                return;
            }

            _renderer.sprite = _frames[frameIndex];
        }

        private void FinishAnimation()
        {
            State finished = _state;
            _state = State.Idle;
            _renderer.sprite = _idleSprite;

            // 장전은 애니메이션이 끝나는 순간에 탄이 들어가야 도중에 무기를 바꿔도 탄이 공짜로 차지 않는다
            if (finished == State.Reloading)
            {
                _ammo.Reload();
                return;
            }

            if (_reloadAfterShot && !_ammo.CanFire)
            {
                TryReload();
            }
        }

        private void Release()
        {
            _ammo.Consume();

            Transform view = _camera.transform;
            Vector3 origin = view.position + view.forward * _spawnDistance - view.up * _spawnDownOffset;

            PlayerProjectile projectile = Instantiate(_projectilePrefab, origin, Quaternion.LookRotation(view.forward));
            int damage = _upgrades != null ? _upgrades.ScaleRangedDamage(_data.Damage) : _data.Damage;
            projectile.Launch(view.forward * _speed, _gravityScale, damage, OnProjectileHit);
        }

        private void OnProjectileHit(bool killed)
        {
            Hit?.Invoke(killed);
        }
    }
}
