using UnityEngine;

namespace ArenaBreak.Core
{
    public enum StageClearCondition
    {
        ReachZone,
        SurviveTime
    }

    [CreateAssetMenu(fileName = "Stage", menuName = "ArenaBreak/Stage Data")]
    public class StageData : ScriptableObject
    {
        [SerializeField] private string _displayName = "스테이지";
        [SerializeField] private StageClearCondition _clearCondition = StageClearCondition.ReachZone;

        [Tooltip("SurviveTime일 때만 쓴다")]
        [SerializeField] private float _surviveSeconds = 120f;

        // 스테이지 3처럼 같은 맵을 시간대만 바꿔 다시 쓰므로 씬 대신 조명 값을 데이터로 들고 있다
        [Header("Lighting")]
        [SerializeField] private Material _skybox;
        [SerializeField] private Color _ambientColor = Color.white;
        [SerializeField] private Color _sunColor = Color.white;
        [SerializeField] private float _sunIntensity = 1f;
        [SerializeField] private Vector3 _sunEulerAngles = new Vector3(50f, -30f, 0f);

        [Header("Drops")]
        [Tooltip("이 스테이지에서 적이 떨어뜨릴 수 있는 무기 표")]
        [SerializeField] private WeaponDropTable _weaponDrops;

        public string DisplayName => _displayName;
        public WeaponDropTable WeaponDrops => _weaponDrops;
        public StageClearCondition ClearCondition => _clearCondition;
        public float SurviveSeconds => _surviveSeconds;
        public Material Skybox => _skybox;
        public Color AmbientColor => _ambientColor;
        public Color SunColor => _sunColor;
        public float SunIntensity => _sunIntensity;
        public Vector3 SunEulerAngles => _sunEulerAngles;

        private void OnValidate()
        {
            _surviveSeconds = Mathf.Max(1f, _surviveSeconds);
            _sunIntensity = Mathf.Max(0f, _sunIntensity);
        }
    }
}
