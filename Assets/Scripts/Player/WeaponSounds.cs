using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    // 무기마다 스크립트가 달라(총·근접·활·투척) 소리를 각 무기에 넣지 않고, 플레이어 한 곳에서 이벤트만 듣고
    // 지금 든 무기의 데이터에서 소리를 고른다. 소리를 바꿀 때 코드나 팔 오브젝트를 건드리지 않아도 된다
    [RequireComponent(typeof(WeaponInventory))]
    public class WeaponSounds : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _attackVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _reloadVolume = 0.6f;

        [Header("Hit")]
        [SerializeField] private AudioClip _hitSound;
        [SerializeField] private AudioClip _killSound;
        [SerializeField, Range(0f, 1f)] private float _hitVolume = 0.7f;

        private WeaponInventory _inventory;
        private GunWeapon[] _guns;
        private KnifeAttackAnimation[] _swings;
        private KnifeDamage[] _melee;
        private ProjectileWeapon[] _projectiles;
        private DynamiteWeapon[] _dynamites;

        private void Awake()
        {
            _inventory = GetComponent<WeaponInventory>();

            // 들지 않은 무기의 팔은 꺼져 있으므로 꺼진 것까지 찾아 미리 구독해 둔다
            _guns = GetComponentsInChildren<GunWeapon>(true);
            _swings = GetComponentsInChildren<KnifeAttackAnimation>(true);
            _melee = GetComponentsInChildren<KnifeDamage>(true);
            _projectiles = GetComponentsInChildren<ProjectileWeapon>(true);
            _dynamites = GetComponentsInChildren<DynamiteWeapon>(true);
        }

        private void OnEnable()
        {
            foreach (GunWeapon gun in _guns)
            {
                gun.Fired += OnAttack;
                gun.OffhandFired += OnAttack;
                gun.ReloadStarted += OnReload;
                gun.Hit += OnHit;
            }

            foreach (KnifeAttackAnimation swing in _swings)
            {
                swing.Swung += OnAttack;
            }

            foreach (KnifeDamage melee in _melee)
            {
                melee.Hit += OnHit;
            }

            foreach (ProjectileWeapon projectile in _projectiles)
            {
                projectile.Fired += OnAttack;
                projectile.ReloadStarted += OnReload;
                projectile.Hit += OnHit;
            }

            foreach (DynamiteWeapon dynamite in _dynamites)
            {
                dynamite.Thrown += OnAttack;
                dynamite.Hit += OnHit;
            }
        }

        private void OnDisable()
        {
            foreach (GunWeapon gun in _guns)
            {
                gun.Fired -= OnAttack;
                gun.OffhandFired -= OnAttack;
                gun.ReloadStarted -= OnReload;
                gun.Hit -= OnHit;
            }

            foreach (KnifeAttackAnimation swing in _swings)
            {
                swing.Swung -= OnAttack;
            }

            foreach (KnifeDamage melee in _melee)
            {
                melee.Hit -= OnHit;
            }

            foreach (ProjectileWeapon projectile in _projectiles)
            {
                projectile.Fired -= OnAttack;
                projectile.ReloadStarted -= OnReload;
                projectile.Hit -= OnHit;
            }

            foreach (DynamiteWeapon dynamite in _dynamites)
            {
                dynamite.Thrown -= OnAttack;
                dynamite.Hit -= OnHit;
            }
        }

        // 이벤트는 켜져 있는 무기, 곧 지금 든 무기에서만 오므로 선택된 슬롯의 데이터를 쓰면 된다
        private WeaponData CurrentWeapon => _inventory.WeaponAt(_inventory.SelectedIndex);

        private void OnAttack()
        {
            SfxPlayer.Play2D(CurrentWeapon.AttackSound, _attackVolume);
        }

        private void OnReload()
        {
            SfxPlayer.Play2D(CurrentWeapon.ReloadSound, _reloadVolume);
        }

        private void OnHit(bool killed)
        {
            SfxPlayer.Play2D(killed && _killSound != null ? _killSound : _hitSound, _hitVolume);
        }
    }
}
