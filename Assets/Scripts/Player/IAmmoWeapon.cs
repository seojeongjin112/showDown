using ArenaBreak.Core;

namespace ArenaBreak.Player
{
    // 총과 다이너마이트처럼 탄약을 쓰는 팔이 인벤토리에서 같은 방식으로 탄약 상태를 받도록 묶는다
    public interface IAmmoWeapon
    {
        void Bind(WeaponAmmo ammo);
    }
}
