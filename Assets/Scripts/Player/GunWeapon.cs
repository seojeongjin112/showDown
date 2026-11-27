using System;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace ArenaBreak.Player
{
    // 칼의 좌클릭 입력과 같은 방식으로 발사하고, R로 장전한다
    [RequireComponent(typeof(SpriteRenderer))]
    public class GunWeapon : MonoBehaviour, IAmmoWeapon
    {
        // 입력 에셋의 Player 맵에는 공격과 장전 액션이 없어서 여기서 직접 바인딩한다
        private const string FireBinding = "<Mouse>/leftButton";
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
        [Tooltip("비어 있으면 한 손 무기다. 있으면 클릭할 때마다 오른손과 이 손이 번갈아 한 발씩 쏜다")]
        [SerializeField] private SpriteRenderer _offhandRenderer;
        [SerializeField] private Sprite[] _offhandFireFrames;
        [SerializeField] private Sprite[] _offhandReloadFrames;

        private SpriteRenderer _renderer;
        private PlayerUpgrades _upgrades;
        private InputAction _fireAction;
        private InputAction _reloadAction;
        private Sprite _idleSprite;
        private WeaponAmmo _ammo;
        private const float FireSlowHoldSeconds = 0.3f;

        private FirstPersonMovement _movement;
        private float _slowUntil;
        private State _state;
        private Sprite[] _frames;
        private float _elapsed;
        private Sprite _offhandIdleSprite;
        private Sprite[] _offhandFrames;
        private float _offhandElapsed;
        private bool _nextShotIsOffhand;
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[HitBufferSize];

        // 명중 표시(HUD)가 총 구현을 몰라도 구독할 수 있게 열어 둔다. 인자는 처치 여부
        public event Action<bool> Hit;

        // 총구 화염처럼 발사 순간에 맞춰 재생하는 연출이 입력을 중복으로 읽지 않고 구독하도록 알린다
        public event Action Fired;

        // 왼손이 쏠 때 따로 알린다. 총구 화염이 손마다 따로 터져야 해서 Fired와 나눴다
        public event Action OffhandFired;

        private bool HasOffhand => _offhandRenderer != null;

        private float CurrentFramesPerSecond => _state == State.Reloading
            ? _data.ReloadFramesPerSecond * (_upgrades != null ? _upgrades.ReloadSpeedMultiplier : 1f)
            : _data.FramesPerSecond;

        // 한쪽 손의 동작이 끝나기 전에 다른 쪽 손이 이어서 움직이는 중이면 무기 전체를 아직 쓰는 중으로 본다
        private bool IsOffhandBusy => _offhandFrames != null;

        // 팔이 꺼져 있어도 탄약을 읽고 쓸 수 있도록 탄약 상태는 인벤토리가 만들어 넘겨 준다
        public void Bind(WeaponAmmo ammo)
        {
            _ammo = ammo;
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _idleSprite = _renderer.sprite;
            // 팔은 플레이어 아래에 있어서 위로 올라가며 찾는다. 카드로 오른 피해와 장전 속도를 읽는다
            _upgrades = GetComponentInParent<PlayerUpgrades>();
            _movement = GetComponentInParent<FirstPersonMovement>();

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

            // 사격 중에 무기를 바꾸면 느린 속도와 점프 금지가 남지 않도록 되돌린다
            _slowUntil = 0f;
            if (_movement != null)
            {
                _movement.FiringSpeedMultiplier = 1f;
                _movement.JumpBlocked = false;
            }

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
            ApplyFiringSlow();

            if (_state == State.Idle)
            {
                // 왼손만 쏜 경우 오른손 동작 상태가 없어서 왼손 그림은 여기서 따로 진행한다
                AdvanceOffhand(Time.deltaTime);

                // 탄창이 비면 따로 누르지 않아도 장전한다. 예비 탄약이 없으면 장전할 수 없어서 아무 일도 없다
                if (_ammo != null && !_ammo.CanFire && _ammo.CanReload && !IsOffhandBusy)
                {
                    TryReload();
                    return;
                }

                ReadInput();
                return;
            }

            Animate();
        }

        // 연사 사이의 짧은 틈에도 속도가 들쭉날쭉하지 않도록 마지막 발사 뒤 잠깐은 느린 채로 둔다
        private void ApplyFiringSlow()
        {
            if (_movement == null)
            {
                return;
            }

            bool slowed = _data.SlowWhileFiring && Time.time < _slowUntil;
            _movement.FiringSpeedMultiplier = slowed ? _data.FiringMoveMultiplier : 1f;
            _movement.JumpBlocked = slowed;
        }

        private void ReadInput()
        {
            // 커서가 풀린 동안의 클릭은 시점 재잠금용이므로 발사로 쓰지 않는다
            if (_ammo == null || !FirstPersonLook.AttackAllowed)
            {
                return;
            }

            // 양손 무기는 한 손이 동작 중이면 다음 클릭을 받지 않아 좌우가 번갈아 한 발씩만 나간다
            if (IsOffhandBusy)
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

            // 양손 무기는 클릭 한 번에 한 발만 쏘고, 오른손과 왼손이 번갈아 쏜다
            if (HasOffhand && _nextShotIsOffhand)
            {
                _nextShotIsOffhand = false;
                FireOffhand();
                return;
            }

            _nextShotIsOffhand = HasOffhand;
            _ammo.Consume();
            _slowUntil = Time.time + FireSlowHoldSeconds;
            Shoot();
            Play(State.Firing, _data.FireFrames);
            Fired?.Invoke();
            FlashMuzzle(0.15f);
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

                // 산탄총은 가까울수록 알갱이 피해가 크다. 다른 총은 거리별 피해가 꺼져 있어 항상 기본 피해다
                float damage = _data.DamageAt(hit.distance);
                damageable.TakeDamage(_upgrades != null ? _upgrades.ScaleRangedDamage(damage) : Mathf.Max(1, Mathf.RoundToInt(damage)));
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

                // 조우 구역·얕은 물 같은 트리거 영역은 물체가 아니라서 탄이 거기서 멈추면 안 된다. 피해를 받는 대상만 트리거여도 맞는다
                if (candidate.collider.isTrigger && candidate.collider.GetComponentInParent<IDamageable>() == null)
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
            if (!_ammo.CanFire)
            {
                return;
            }

            _ammo.Consume();
            Shoot();
            StartOffhand(_offhandFireFrames);
            OffhandFired?.Invoke();
            FlashMuzzle(-0.15f);
        }

        // 쏘는 순간 주변 벽과 적이 잠깐 환해져 총소리와 함께 타격감을 준다
        private void FlashMuzzle(float sideOffset)
        {
            Transform view = _camera.transform;
            Vector3 position = view.position + view.forward * 1.5f + view.right * sideOffset;
            ArenaLighting.Flash(position, new Color(1f, 0.75f, 0.35f), _data.Pellets > 1 ? 1.5f : 1.0f, 9f, 0.1f);
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
            _offhandFrames = null;

            if (HasOffhand)
            {
                _offhandRenderer.sprite = _offhandIdleSprite;
            }
        }
    }
}
