using System;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace ArenaBreak.Player
{
    // 칼의 우클릭 입력과 같은 방식으로 발사하고, R로 장전한다
    [RequireComponent(typeof(SpriteRenderer))]
    public class GunWeapon : MonoBehaviour, IAmmoWeapon
    {
        // 입력 에셋의 Player 맵에는 우클릭과 장전 액션이 없어서 여기서 직접 바인딩한다
        private const string FireBinding = "<Mouse>/rightButton";
        private const string ReloadBinding = "<Keyboard>/r";
        // 한 광선이 겹쳐 지나는 콜라이더 수의 상한이라 넉넉히 잡는다
        private const int HitBufferSize = 16;

        private enum State
        {
            Idle,
            Firing,
            Reloading
        }

        [SerializeField] private WeaponData _data;
        [SerializeField] private Camera _camera;

        [Header("양손 (양손 리볼버만)")]
        [Tooltip("비어 있으면 한 손 무기다. 있으면 오른손이 쏜 뒤 조금 있다가 이 손도 쏜다")]
        [SerializeField] private SpriteRenderer _offhandRenderer;
        [SerializeField] private Sprite[] _offhandFireFrames;
        [SerializeField] private Sprite[] _offhandReloadFrames;
        [Tooltip("오른손이 쏜 뒤 이 손이 쏠 때까지의 시간. 두 발이 한 번에 터지면 한 발처럼 느껴져서 어긋나게 한다")]
        [SerializeField] private float _offhandDelay = 0.12f;

        private SpriteRenderer _renderer;
        private InputAction _fireAction;
        private InputAction _reloadAction;
        private Sprite _idleSprite;
        private WeaponAmmo _ammo;
        private State _state;
        private Sprite[] _frames;
        private float _elapsed;
        private Sprite _offhandIdleSprite;
        private Sprite[] _offhandFrames;
        private float _offhandElapsed;
        private float _offhandWait;
        private bool _offhandPending;
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[HitBufferSize];

        // 명중 표시(HUD)가 총 구현을 몰라도 구독할 수 있게 열어 둔다. 인자는 처치 여부
        public event Action<bool> Hit;

        // 총구 화염처럼 발사 순간에 맞춰 재생하는 연출이 입력을 중복으로 읽지 않고 구독하도록 알린다
        public event Action Fired;

        // 왼손이 쏠 때 따로 알린다. 총구 화염이 손마다 따로 터져야 해서 Fired와 나눴다
        public event Action OffhandFired;

        private bool HasOffhand => _offhandRenderer != null;

        private float CurrentFramesPerSecond => _state == State.Reloading ? _data.ReloadFramesPerSecond : _data.FramesPerSecond;

        // 한쪽 손의 동작이 끝나기 전에 다른 쪽 손이 이어서 움직이는 중이면 무기 전체를 아직 쓰는 중으로 본다
        private bool IsOffhandBusy => _offhandPending || _offhandFrames != null;

        // 팔이 꺼져 있어도 탄약을 읽고 쓸 수 있도록 탄약 상태는 인벤토리가 만들어 넘겨 준다
        public void Bind(WeaponAmmo ammo)
        {
            _ammo = ammo;
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _idleSprite = _renderer.sprite;

            if (_offhandRenderer != null)
            {
                _offhandIdleSprite = _offhandRenderer.sprite;
            }

            if (_camera == null)
            {
                _camera = GetComponentInParent<Camera>();
            }

            if (_data == null || _camera == null)
            {
                Debug.LogError("GunWeapon: 무기 데이터와 카메라 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            _fireAction = new InputAction("GunFire", InputActionType.Button, FireBinding);
            _reloadAction = new InputAction("GunReload", InputActionType.Button, ReloadBinding);
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

            // 장전이나 발사 도중에 무기를 바꿔도 다음에 꺼냈을 때 중간 자세로 굳어 있지 않도록 되돌린다
            _state = State.Idle;
            _renderer.sprite = _idleSprite;
            ResetOffhand();
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

            // 연사 무기는 누르고 있는 동안 동작이 끝날 때마다 다음 발을 쏜다
            bool firePressed = _data.Automatic ? _fireAction.IsPressed() : _fireAction.WasPressedThisFrame();

            if (firePressed)
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

            _ammo.Consume();
            Shoot();
            Play(State.Firing, _data.FireFrames);
            Fired?.Invoke();

            // 마지막 한 발이면 왼손은 쏠 탄이 없다
            if (HasOffhand && _ammo.CanFire)
            {
                _offhandPending = true;
                _offhandWait = _offhandDelay;
            }
        }

        private void TryReload()
        {
            if (_ammo.CanReload)
            {
                Play(State.Reloading, _data.ReloadFrames);

                if (HasOffhand)
                {
                    StartOffhand(_offhandReloadFrames);
                }
            }
        }

        private void Shoot()
        {
            Transform view = _camera.transform;
            bool anyHit = false;
            bool anyKill = false;

            for (int i = 0; i < _data.Pellets; i++)
            {
                // 알갱이마다 원뿔 안에서 방향을 흩어야 산탄이 한 점으로 모이지 않는다
                Vector2 jitter = Random.insideUnitCircle * _data.SpreadDegrees;
                Vector3 direction = view.rotation * Quaternion.Euler(jitter.y, jitter.x, 0f) * Vector3.forward;

                if (!TryRaycastIgnoringSelf(view, direction, out RaycastHit hit))
                {
                    continue;
                }

                // 건물 같은 장애물이 먼저 맞으면 피해를 받는 대상이 아니라서 알갱이가 거기서 멈춘다
                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable == null)
                {
                    continue;
                }

                damageable.TakeDamage(_data.Damage);
                anyHit = true;
                anyKill |= damageable is Health health && health.IsDead;
            }

            // 알갱이마다 표시하면 산탄 한 발에 명중 표시가 여러 번 깜빡이므로 발사 한 번에 한 번만 알린다
            if (anyHit)
            {
                Hit?.Invoke(anyKill);
            }
        }

        // 아래를 보고 쏘면 광선이 플레이어 자신의 콜라이더를 먼저 맞아 자해가 되므로 자기 몸은 건너뛴다
        private bool TryRaycastIgnoringSelf(Transform view, Vector3 direction, out RaycastHit nearest)
        {
            int count = Physics.RaycastNonAlloc(view.position, direction, _hitBuffer, _data.Range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            nearest = default;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = _hitBuffer[i];
                if (candidate.transform.IsChildOf(view.root))
                {
                    continue;
                }

                if (!found || candidate.distance < nearest.distance)
                {
                    nearest = candidate;
                    found = true;
                }
            }

            return found;
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
            AdvanceOffhand(Time.deltaTime);
            int frameIndex = (int)(_elapsed * CurrentFramesPerSecond);

            if (frameIndex >= _frames.Length && IsOffhandBusy)
            {
                // 이 손은 끝났지만 다른 손이 아직 움직이는 중이라 마지막 자세로 기다린다
                _renderer.sprite = _frames[_frames.Length - 1];
                return;
            }

            if (frameIndex >= _frames.Length)
            {
                // 장전은 애니메이션이 끝나는 순간에 탄이 들어가야 도중에 무기를 바꿔도 탄이 공짜로 차지 않는다
                if (_state == State.Reloading)
                {
                    _ammo.Reload();
                }

                _state = State.Idle;
                _renderer.sprite = _idleSprite;
                return;
            }

            _renderer.sprite = _frames[frameIndex];
        }

        private void AdvanceOffhand(float delta)
        {
            if (!HasOffhand)
            {
                return;
            }

            if (_offhandPending)
            {
                _offhandWait -= delta;
                if (_offhandWait <= 0f)
                {
                    _offhandPending = false;
                    FireOffhand();
                }
            }

            if (_offhandFrames == null)
            {
                return;
            }

            _offhandElapsed += delta;
            int index = (int)(_offhandElapsed * CurrentFramesPerSecond);

            if (index >= _offhandFrames.Length)
            {
                _offhandFrames = null;
                _offhandRenderer.sprite = _offhandIdleSprite;
                return;
            }

            _offhandRenderer.sprite = _offhandFrames[index];
        }

        private void FireOffhand()
        {
            // 기다리는 사이에 탄이 바닥났을 수 있다
            if (!_ammo.CanFire)
            {
                return;
            }

            _ammo.Consume();
            Shoot();
            StartOffhand(_offhandFireFrames);
            OffhandFired?.Invoke();
        }

        private void StartOffhand(Sprite[] frames)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            _offhandFrames = frames;
            _offhandElapsed = 0f;
            _offhandRenderer.sprite = frames[0];
        }

        // 발사나 장전 도중에 무기를 바꿔도 다음에 꺼냈을 때 왼손이 중간 자세로 굳어 있지 않도록 되돌린다
        private void ResetOffhand()
        {
            _offhandPending = false;
            _offhandFrames = null;

            if (HasOffhand)
            {
                _offhandRenderer.sprite = _offhandIdleSprite;
            }
        }
    }
}
