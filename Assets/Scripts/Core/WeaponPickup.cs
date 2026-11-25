using System;
using ArenaBreak.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Core
{
    // 적이 가끔 떨어뜨리므로 생성·파괴가 잦지 않아 풀링 없이 둔다
    public class WeaponPickup : Pickup
    {
        [SerializeField] private SpriteRenderer _icon;
        [Tooltip("그림의 긴 변이 이 길이가 되도록 크기를 맞춘다. 무기 그림마다 원본 크기가 달라서다")]
        [SerializeField] private float _iconSize = 1.6f;
        [SerializeField] private float _framesPerSecond = 10f;

        // 같은 종류의 무기를 이미 들고 있어서 바꿀지 묻는 중인 아이템. 한 번에 하나만 묻는다
        public static WeaponPickup Pending { get; private set; }

        // 안내판이 폴링하지 않고 구독해서 묻는 중에만 띄운다
        public static event Action<WeaponPickup> PendingChanged;

        private WeaponInventory _inventory;
        private WeaponData _weapon;
        private bool _declined;
        private int _ammoWhenOwned;
        // 새 무기일 때만 돈다. 탄약으로 떨어진 것은 한 장짜리 그림이다
        private Sprite[] _frames;
        private float _elapsed;

        protected override void Awake()
        {
            base.Awake();

            if (Player == null)
            {
                return;
            }

            if (!Player.TryGetComponent(out _inventory) || _icon == null)
            {
                Debug.LogError("WeaponPickup: Player에 WeaponInventory가 없거나 그림 참조가 비어 있습니다", this);
                enabled = false;
            }
        }

        public WeaponData Weapon => _weapon;

        // 교체하면 내려놓게 될 지금 든 무기. 없으면 null
        public WeaponData ReplacedWeapon
        {
            get
            {
                int index = _inventory != null && _weapon != null ? _inventory.OwnedIndexIn(_weapon.SlotType) : -1;
                return index >= 0 ? _inventory.WeaponAt(index) : null;
            }
        }

        public void Setup(WeaponDrop drop)
        {
            _weapon = drop.Weapon;
            _ammoWhenOwned = drop.AmmoWhenOwned;

            // 이미 가진 무기는 탄약 그림으로 보여 줘야 무엇을 줍는지 알 수 있다
            bool isNew = _inventory.CanUnlock(_weapon);
            bool hasFrames = drop.Frames != null && drop.Frames.Length > 0;

            if (isNew && hasFrames)
            {
                _frames = drop.Frames;
                ApplyIcon(_frames[0]);
            }
            else
            {
                _frames = null;
                ApplyIcon(!isNew && _weapon.AmmoIcon != null ? _weapon.AmmoIcon : _weapon.Icon);
            }
        }

        // 같은 종류의 무기를 이미 들고 있으면 닿자마자 바꾸지 않고 물어본다. E로 교체하고 Q나 자리를 떠나면 그대로 둔다
        protected override bool TryPickUp()
        {
            bool replacing = _inventory.CanUnlock(_weapon) && _inventory.OwnedIndexIn(_weapon.SlotType) >= 0;
            if (!replacing)
            {
                OnPickedUp();
                return true;
            }

            if (_declined || (Pending != null && Pending != this))
            {
                return false;
            }

            SetPending(this);

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                SetPending(null);
                OnPickedUp();
                return true;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                _declined = true;
                SetPending(null);
            }

            return false;
        }

        // 자리를 벗어나면 거절 기록을 지워서 다시 다가가면 또 물어본다
        protected override void OnOutOfRange()
        {
            _declined = false;

            if (Pending == this)
            {
                SetPending(null);
            }
        }

        private void OnDisable()
        {
            if (Pending == this)
            {
                SetPending(null);
            }
        }

        private static void SetPending(WeaponPickup pickup)
        {
            if (Pending == pickup)
            {
                return;
            }

            Pending = pickup;
            PendingChanged?.Invoke(pickup);
        }

        // 떨어진 뒤에 다른 경로로 무기를 얻었을 수 있어서 줍는 순간에 다시 판단한다
        protected override void OnPickedUp()
        {
            if (_inventory.CanUnlock(_weapon))
            {
                _inventory.Unlock(_weapon);
            }
            else
            {
                _inventory.AddAmmo(_weapon, _ammoWhenOwned);
            }
        }

        // Pickup이 비공개 Update로 줍기를 처리하므로, 그림 돌리기는 LateUpdate에서 따로 한다
        private void LateUpdate()
        {
            if (_frames == null)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            _icon.sprite = _frames[(int)(_elapsed * _framesPerSecond) % _frames.Length];
        }

        private void ApplyIcon(Sprite sprite)
        {
            _icon.sprite = sprite;
            Vector3 size = sprite.bounds.size;
            float scale = _iconSize / Mathf.Max(size.x, size.y);
            _icon.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
