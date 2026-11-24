using System;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    // 우클릭으로 던지고, 손을 떠나는 프레임에 투척물을 만든다
    [RequireComponent(typeof(SpriteRenderer))]
    public class DynamiteWeapon : MonoBehaviour, IAmmoWeapon
    {
        // 입력 에셋의 Player 맵에는 우클릭 액션이 없어서 여기서 직접 바인딩한다
        private const string FireBinding = "<Mouse>/rightButton";

        [SerializeField] private WeaponData _data;
        [SerializeField] private Camera _camera;
        [SerializeField] private Dynamite _dynamitePrefab;
        [Tooltip("던지는 애니메이션에서 손을 떠나는 프레임 번호")]
        [SerializeField] private int _releaseFrame = 6;
        [SerializeField] private float _throwSpeed = 14f;
        // 약간 위로 던져야 조준점 앞쪽에 포물선으로 떨어진다
        [SerializeField] private float _upwardBias = 0.2f;
        [SerializeField] private float _spawnDistance = 0.6f;

        private SpriteRenderer _renderer;
        private PlayerUpgrades _upgrades;
        private InputAction _fireAction;
        private Sprite _idleSprite;
        private WeaponAmmo _ammo;
        private bool _isThrowing;
        private bool _released;
        private float _elapsed;

        // 명중 표시(HUD)가 던지기 구현을 몰라도 구독할 수 있게 열어 둔다. 인자는 처치 여부
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

            if (_data == null || _camera == null || _dynamitePrefab == null)
            {
                Debug.LogError("DynamiteWeapon: 무기 데이터, 카메라, 다이너마이트 프리팹 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            _fireAction = new InputAction("DynamiteThrow", InputActionType.Button, FireBinding);
        }

        private void OnEnable()
        {
            _fireAction.Enable();
        }

        private void OnDisable()
        {
            if (_fireAction == null)
            {
                return;
            }

            _fireAction.Disable();

            // 던지기 도중에 무기를 바꾸면 아직 던지지 않았으므로 탄약은 그대로 두고 처음 자세로 되돌린다
            _isThrowing = false;
            _renderer.sprite = _idleSprite;
        }

        private void OnDestroy()
        {
            _fireAction?.Dispose();
        }

        private void Update()
        {
            if (!_isThrowing)
            {
                if (_ammo == null)
                {
                    return;
                }

                // 다 던진 뒤에 보급을 사면 쥐고 있는 것이 없어 영영 못 던지므로, 비어 있고 예비가 있으면 바로 쥔다
                if (!_ammo.CanFire && _ammo.CanReload)
                {
                    _ammo.Reload();
                }

                // 커서가 풀린 동안의 클릭은 시점 재잠금용이므로 던지기로 쓰지 않는다
                if (_ammo.CanFire && Cursor.lockState == CursorLockMode.Locked && _fireAction.WasPressedThisFrame())
                {
                    _isThrowing = true;
                    _released = false;
                    _elapsed = 0f;
                    _renderer.sprite = _data.FireFrames[0];
                }

                return;
            }

            Animate();
        }

        private void Animate()
        {
            _elapsed += Time.deltaTime;
            int frameIndex = (int)(_elapsed * _data.FramesPerSecond);
            Sprite[] frames = _data.FireFrames;

            if (!_released && frameIndex >= _releaseFrame)
            {
                _released = true;
                Throw();
            }

            if (frameIndex >= frames.Length)
            {
                _isThrowing = false;
                _renderer.sprite = _idleSprite;

                // 던질 때마다 따로 장전하지 않아도 되도록 다음 한 개를 바로 쥔다
                if (_ammo.CanReload)
                {
                    _ammo.Reload();
                }

                return;
            }

            _renderer.sprite = frames[frameIndex];
        }

        private void Throw()
        {
            _ammo.Consume();

            Transform view = _camera.transform;
            Vector3 direction = (view.forward + view.up * _upwardBias).normalized;

            Dynamite dynamite = Instantiate(_dynamitePrefab, view.position + view.forward * _spawnDistance, Quaternion.identity);
            int damage = _upgrades != null ? _upgrades.ScaleRangedDamage(_data.Damage) : _data.Damage;
            dynamite.Launch(direction * _throwSpeed, damage, OnBlastHit);
        }

        private void OnBlastHit(bool killed)
        {
            Hit?.Invoke(killed);
        }
    }
}
