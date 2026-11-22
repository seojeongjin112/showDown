using UnityEngine;

namespace ArenaBreak.Core
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "ArenaBreak/Weapon")]
    public class WeaponData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [Tooltip("무기 슬롯에 표시하는 그림")]
        [SerializeField] private Sprite _icon;

        [Header("총 전용 (칼은 탄창 크기를 0으로 둔다)")]
        [SerializeField] private Sprite _ammoIcon;
        [SerializeField] private int _damage = 2;
        [SerializeField] private float _range = 40f;
        [SerializeField] private int _magazineSize;
        [SerializeField] private int _startReserve;
        [SerializeField] private int _maxReserve;
        [SerializeField] private Sprite[] _fireFrames;
        [SerializeField] private Sprite[] _reloadFrames;
        [SerializeField] private float _framesPerSecond = 18f;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public Sprite AmmoIcon => _ammoIcon;
        public int Damage => _damage;
        public float Range => _range;
        public int MagazineSize => _magazineSize;
        public int StartReserve => _startReserve;
        public int MaxReserve => _maxReserve;
        public Sprite[] FireFrames => _fireFrames;
        public Sprite[] ReloadFrames => _reloadFrames;
        public float FramesPerSecond => _framesPerSecond;

        public bool UsesAmmo => _magazineSize > 0;
    }
}
