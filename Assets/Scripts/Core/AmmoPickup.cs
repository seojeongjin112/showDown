using ArenaBreak.Player;
using UnityEngine;

namespace ArenaBreak.Core
{
    // 근접으로 처치할 때만 나오는 탄약 꾸러미. 쏘기만 해서는 탄약이 안 들어오게 해 근접전을 섞게 만든다.
    // 적을 처치할 때마다 생성·파괴가 반복되므로 풀링 대상
    public class AmmoPickup : Pickup
    {
        [SerializeField] private SpriteRenderer _icon;
        [Tooltip("그림의 긴 변이 이 길이가 되도록 크기를 맞춘다. 탄약 아이콘이 작게 그려져 있어서다")]
        [SerializeField] private float _iconSize = 0.9f;

        private WeaponInventory _inventory;
        private WeaponData _weapon;

        protected override void Awake()
        {
            base.Awake();

            if (Player == null)
            {
                return;
            }

            if (!Player.TryGetComponent(out _inventory) || _icon == null)
            {
                Debug.LogError("AmmoPickup: Player에 WeaponInventory가 없거나 그림 참조가 비어 있습니다", this);
                enabled = false;
            }
        }

        public void Setup(WeaponData weapon)
        {
            _weapon = weapon;

            Sprite sprite = weapon.AmmoIcon != null ? weapon.AmmoIcon : weapon.Icon;
            _icon.sprite = sprite;

            Vector3 size = sprite.bounds.size;
            float scale = _iconSize / Mathf.Max(size.x, size.y);
            _icon.transform.localScale = new Vector3(scale, scale, 1f);
        }

        // 떨어진 뒤에 그 무기를 내려놓았으면 줘도 쓸 곳이 없으므로 아무 일도 없다
        protected override void OnPickedUp()
        {
            _inventory.AddAmmo(_weapon, _weapon.AmmoPackSize);
        }
    }
}
