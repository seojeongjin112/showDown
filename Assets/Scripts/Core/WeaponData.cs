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
        [Tooltip("한 발에 나가는 알갱이 수. 산탄총은 여러 개, 나머지는 1")]
        [SerializeField] private int _pellets = 1;
        [Tooltip("알갱이가 조준선에서 퍼지는 최대 각도(도). 0이면 정확히 조준선으로 나간다")]
        [SerializeField] private float _spreadDegrees;
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
        public int Pellets => _pellets;
        public float SpreadDegrees => _spreadDegrees;
        public int MagazineSize => _magazineSize;
        public int StartReserve => _startReserve;
        public int MaxReserve => _maxReserve;
        public Sprite[] FireFrames => _fireFrames;
        public Sprite[] ReloadFrames => _reloadFrames;
        public float FramesPerSecond => _framesPerSecond;

        public bool UsesAmmo => _magazineSize > 0;
    }
}
