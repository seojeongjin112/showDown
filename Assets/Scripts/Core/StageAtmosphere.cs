using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ArenaBreak.Core
{
    // 스테이지가 바뀔 때 화면 전체의 색 보정과 비네트 세기를 바꿔 분위기를 낸다
    public class StageAtmosphere : MonoBehaviour
    {
        [System.Serializable]
        private struct Look
        {
            [SerializeField] private Color _colorFilter;
            [SerializeField] private float _saturation;
            [SerializeField, Range(0f, 1f)] private float _vignette;
            [SerializeField] private float _bloomIntensity;

            public Color ColorFilter => _colorFilter;
            public float Saturation => _saturation;
            public float Vignette => _vignette;
            public float BloomIntensity => _bloomIntensity;
        }

        [SerializeField] private StageManager _stages;
        [SerializeField] private Volume _volume;
        [Tooltip("스테이지 순서대로 1, 2, 3. 목록보다 스테이지가 많으면 마지막 값을 쓴다")]
        [SerializeField] private Look[] _looks;

        private void Awake()
        {
            if (_stages == null || _volume == null || _looks == null || _looks.Length == 0)
            {
                Debug.LogError("StageAtmosphere: 스테이지 관리자, 볼륨, 또는 분위기 목록이 비어 있습니다", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _stages.StageStarted += OnStageStarted;
        }

        private void OnDisable()
        {
            _stages.StageStarted -= OnStageStarted;
        }

        // 프로필 사본은 볼륨이 다시 만들 수 있어서 값을 쓸 때마다 새로 찾는다
        private void OnStageStarted(int stage)
        {
            Look look = _looks[Mathf.Clamp(stage - 1, 0, _looks.Length - 1)];
            VolumeProfile profile = _volume.profile;

            if (profile.TryGet(out ColorAdjustments color))
            {
                color.colorFilter.value = look.ColorFilter;
                color.saturation.value = look.Saturation;
            }

            if (profile.TryGet(out Vignette vignette))
            {
                vignette.intensity.value = look.Vignette;
            }

            if (profile.TryGet(out Bloom bloom))
            {
                bloom.intensity.value = look.BloomIntensity;
            }
        }
    }
}
