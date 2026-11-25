using UnityEngine;

namespace ArenaBreak.Core
{
    // 번개가 칠 때만 하늘이 흐려지게 한다. 햇빛·앰비언트를 줄이고 하늘 그림을 어둡게 틔워서 구름이 낀 느낌을 낸다
    public class StormSky : MonoBehaviour
    {
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int TintId = Shader.PropertyToID("_Tint");

        [SerializeField] private Light _sun;
        [SerializeField] private float _fadeInSeconds = 0.8f;
        [SerializeField] private float _fadeOutSeconds = 1.6f;
        [Tooltip("폭풍우일 때 햇빛 세기에 곱한다")]
        [SerializeField, Range(0f, 1f)] private float _sunMultiplier = 0.3f;
        [Tooltip("폭풍우일 때 앰비언트 밝기에 곱한다")]
        [SerializeField, Range(0f, 1f)] private float _ambientMultiplier = 0.55f;
        [SerializeField, Range(0f, 1f)] private float _skyExposureMultiplier = 0.4f;
        [SerializeField] private Color _skyTint = new Color(0.45f, 0.5f, 0.62f, 1f);

        private static StormSky s_instance;

        private Material _skyCopy;
        private Material _originalSky;
        private Color _baseSunColor;
        private float _baseSunIntensity;
        private Color _baseAmbient;
        private float _baseSkyExposure;
        private Color _baseSkyTint;
        private bool _stormy;
        private float _amount;
        private bool _applied;

        // 보스가 어느 씬 오브젝트도 모르고 부를 수 있게 정적으로 연다. 씬에 이 컴포넌트가 없으면 아무 일도 없다
        public static void SetStorm(bool stormy)
        {
            if (s_instance != null)
            {
                s_instance._stormy = stormy;
            }
        }

        private void Awake()
        {
            s_instance = this;
        }

        private void OnDestroy()
        {
            Restore();

            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        private void Update()
        {
            if (!_stormy && !_applied)
            {
                return;
            }

            if (_stormy && !_applied)
            {
                CaptureBase();
            }

            float seconds = _stormy ? _fadeInSeconds : _fadeOutSeconds;
            _amount = Mathf.MoveTowards(_amount, _stormy ? 1f : 0f, Time.deltaTime / Mathf.Max(seconds, 0.01f));
            Apply(_amount);

            if (!_stormy && _amount <= 0f)
            {
                Restore();
            }
        }

        // 스테이지마다 하늘과 햇빛이 달라서, 폭풍이 시작되는 순간의 값을 기준으로 삼는다
        private void CaptureBase()
        {
            _applied = true;
            _baseSunColor = _sun != null ? _sun.color : Color.white;
            _baseSunIntensity = _sun != null ? _sun.intensity : 0f;
            _baseAmbient = RenderSettings.ambientLight;

            Material sky = RenderSettings.skybox;
            if (sky != null)
            {
                // 하늘 재질 에셋을 직접 바꾸면 폭풍 뒤에도 에디터에 남으므로 사본에만 적용한다
                _originalSky = sky;
                _skyCopy = new Material(sky);
                _baseSkyExposure = sky.HasProperty(ExposureId) ? sky.GetFloat(ExposureId) : 1f;
                _baseSkyTint = sky.HasProperty(TintId) ? sky.GetColor(TintId) : Color.white;
                RenderSettings.skybox = _skyCopy;
            }
        }

        private void Apply(float amount)
        {
            if (_sun != null)
            {
                _sun.intensity = Mathf.Lerp(_baseSunIntensity, _baseSunIntensity * _sunMultiplier, amount);
            }

            Color dimmed = _baseAmbient * _ambientMultiplier;
            dimmed.a = _baseAmbient.a;
            RenderSettings.ambientLight = Color.Lerp(_baseAmbient, dimmed, amount);

            if (_skyCopy != null)
            {
                if (_skyCopy.HasProperty(ExposureId))
                {
                    _skyCopy.SetFloat(ExposureId, Mathf.Lerp(_baseSkyExposure, _baseSkyExposure * _skyExposureMultiplier, amount));
                }

                if (_skyCopy.HasProperty(TintId))
                {
                    _skyCopy.SetColor(TintId, Color.Lerp(_baseSkyTint, _skyTint, amount));
                }
            }
        }

        // 폭풍이 끝나면 원래 하늘 재질로 되돌리고 사본은 버린다
        private void Restore()
        {
            if (!_applied)
            {
                return;
            }

            _applied = false;
            _amount = 0f;

            if (_sun != null)
            {
                _sun.color = _baseSunColor;
                _sun.intensity = _baseSunIntensity;
            }

            RenderSettings.ambientLight = _baseAmbient;

            if (_skyCopy != null)
            {
                // 폭풍 중에 스테이지가 하늘을 바꿨다면 그 하늘을 덮어쓰지 않는다
                if (RenderSettings.skybox == _skyCopy)
                {
                    RenderSettings.skybox = _originalSky;
                }

                Destroy(_skyCopy);
                _skyCopy = null;
            }
        }
    }
}
