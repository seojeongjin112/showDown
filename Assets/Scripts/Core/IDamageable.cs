namespace ArenaBreak.Core
{
    // 공격 쪽이 Health 구현을 몰라도 피해를 줄 수 있게 하는 접점
    public interface IDamageable
    {
        void TakeDamage(int amount);
    }
}
