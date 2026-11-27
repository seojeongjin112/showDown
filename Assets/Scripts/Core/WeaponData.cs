using UnityEngine;

namespace ArenaBreak.Core
{
    // 들고 다니는 무기는 종류마다 한 자루씩이라 새 무기를 얻으면 같은 종류의 기존 무기와 바뀐다.
    // 저장된 에셋의 값이 밀리지 않도록 순서를 바꾸지 않는다
    public enum WeaponSlotType
    {
        Melee,
        Sidearm,
        Primary
    }

    [CreateAssetMenu(fileName = "Weapon", menuName = "ArenaBreak/Weapon")]
    public class WeaponData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [Tooltip("근접은 1번, 보조는 2번, 주 무기는 3번 키로 든다")]
        [SerializeField] private WeaponSlotType _slotType;
        [Tooltip("무기 슬롯에 표시하는 그림")]
        [SerializeField] private Sprite _icon;

        [Header("총 전용 (칼은 탄창 크기를 0으로 둔다)")]
        [SerializeField] private Sprite _ammoIcon;
        [SerializeField] private int _damage = 2;
        [SerializeField] private float _range = 40f;
        [Tooltip("켜면 거리에 따라 피해가 줄어든다. 가까이서 쏠수록 위 피해 그대로 들어가고 멀수록 아래 최소 피해까지 줄어든다")]
        [SerializeField] private bool _distanceFalloff;
        [Tooltip("이 거리까지는 피해가 줄지 않는다")]
        [SerializeField] private float _falloffStartDistance = 4f;
        [Tooltip("이 거리 이상에서는 최소 피해가 들어간다")]
        [SerializeField] private float _falloffEndDistance = 16f;
        [SerializeField] private float _minDamage = 1f;
        [Tooltip("한 발에 나가는 알갱이 수. 산탄총은 여러 개, 나머지는 1")]
        [SerializeField] private int _pellets = 1;
        [Tooltip("알갱이가 조준선에서 퍼지는 최대 각도(도). 0이면 정확히 조준선으로 나간다")]
        [SerializeField] private float _spreadDegrees;
        [Tooltip("켜면 사격하는 동안 이동 속도가 느려지고 점프할 수 없다. 개틀링 같은 무거운 무기용")]
        [SerializeField] private bool _slowWhileFiring;
        [Tooltip("사격 중 이동 속도에 곱한다. 사격을 멈추고 잠깐 뒤에 원래 속도로 돌아온다")]
        [SerializeField, Range(0.1f, 1f)] private float _firingMoveMultiplier = 0.4f;
        [Tooltip("켜면 발사 버튼을 누르고 있는 동안 계속 쏜다. 끄면 누를 때마다 한 발")]
        [SerializeField] private bool _automatic;
        [SerializeField] private int _magazineSize;
        [SerializeField] private int _startReserve;
        [SerializeField] private int _maxReserve;
        [Tooltip("적이 떨어뜨리는 탄약 꾸러미 하나가 주는 예비 탄약")]
        [SerializeField] private int _ammoPackSize = 4;
        [SerializeField] private Sprite[] _fireFrames;
        [SerializeField] private Sprite[] _reloadFrames;
        [SerializeField] private float _framesPerSecond = 18f;
        [Tooltip("0이면 발사와 같은 속도. 연사 무기는 발사를 빠르게 돌리면 장전도 빨라져서 따로 정한다")]
        [SerializeField] private float _reloadFramesPerSecond;

        public string DisplayName => _displayName;
        public WeaponSlotType SlotType => _slotType;
        public Sprite Icon => _icon;
        public Sprite AmmoIcon => _ammoIcon;
        public int Damage => _damage;
        public float Range => _range;

        // 거리별 피해는 정수가 아닐 수 있어서 소수로 돌려주고, 올림·내림은 받는 쪽이 정한다
        public float DamageAt(float distance)
        {
            if (!_distanceFalloff)
            {
                return _damage;
            }

            float t = Mathf.InverseLerp(_falloffStartDistance, Mathf.Max(_falloffEndDistance, _falloffStartDistance + 0.01f), distance);
            return Mathf.Lerp(_damage, Mathf.Min(_minDamage, _damage), t);
        }
        public int Pellets => _pellets;
        public float SpreadDegrees => _spreadDegrees;
        public bool SlowWhileFiring => _slowWhileFiring;
        public float FiringMoveMultiplier => _firingMoveMultiplier;
        public bool Automatic => _automatic;
        public int MagazineSize => _magazineSize;
        public int StartReserve => _startReserve;
        public int MaxReserve => _maxReserve;
        public int AmmoPackSize => _ammoPackSize;
        public Sprite[] FireFrames => _fireFrames;
        public Sprite[] ReloadFrames => _reloadFrames;
        public float FramesPerSecond => _framesPerSecond;
        public float ReloadFramesPerSecond => _reloadFramesPerSecond > 0f ? _reloadFramesPerSecond : _framesPerSecond;

        public bool UsesAmmo => _magazineSize > 0;
    }
}
