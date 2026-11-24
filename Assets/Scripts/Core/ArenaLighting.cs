using UnityEngine;

namespace ArenaBreak.Core
{
    // 햇빛·앰비언트와 가까운 점광 몇 개를 모아 모든 조명 셰이더에 전역 값으로 넘긴다.
    // 에디터에서도 결과가 보이도록 ExecuteAlways로 둔다
    [ExecuteAlways]
    public class ArenaLighting : MonoBehaviour
    {
        private const int MaxLights = 8;
        private const int MaxFlashes = 6;

        private static readonly int LightPosId = Shader.PropertyToID("_ArenaLightPos");
        private static readonly int LightColorId = Shader.PropertyToID("_ArenaLightColor");
        private static readonly int BaseId = Shader.PropertyToID("_ArenaBase");
        private static readonly int ParamsId = Shader.PropertyToID("_ArenaParams");

        // 총구 화염과 폭발은 잠깐 번쩍이고 사라져서 오브젝트를 만들지 않고 배열 칸으로만 둔다
        private static readonly Vector3[] FlashPosition = new Vector3[MaxFlashes];
        private static readonly Color[] FlashColor = new Color[MaxFlashes];
        private static readonly float[] FlashRange = new float[MaxFlashes];
        private static readonly float[] FlashRemaining = new float[MaxFlashes];
        private static readonly float[] FlashDuration = new float[MaxFlashes];
        private static int _nextFlash;

        [SerializeField] private Light _sun;
        [Tooltip("0이면 밝기가 부드럽게 변하고, 3~8이면 도트 그림처럼 몇 단계로 끊긴다")]
        [SerializeField] private int _brightnessSteps = 7;

        private readonly Vector4[] _positions = new Vector4[MaxLights];
        private readonly Vector4[] _colors = new Vector4[MaxLights];
        private readonly float[] _scores = new float[MaxLights];
        private Camera _camera;

        // 발사·폭발 같은 순간 조명. 칸이 가득 차면 가장 오래된 것을 덮어쓴다
        public static void Flash(Vector3 position, Color color, float intensity, float range, float seconds)
        {
            int slot = _nextFlash;
            _nextFlash = (_nextFlash + 1) % MaxFlashes;

            FlashPosition[slot] = position;
            FlashColor[slot] = color * intensity;
            FlashRange[slot] = range;
            FlashRemaining[slot] = seconds;
            FlashDuration[slot] = Mathf.Max(seconds, 0.0001f);
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            Vector3 origin = _camera != null ? _camera.transform.position : transform.position;

            Color ambient = RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat ? RenderSettings.ambientLight : RenderSettings.ambientSkyColor;
            Color sun = _sun != null && _sun.enabled ? _sun.color * _sun.intensity : Color.black;
            Shader.SetGlobalVector(BaseId, new Vector4(ambient.r + sun.r, ambient.g + sun.g, ambient.b + sun.b, 1f));

            int count = CollectLights(origin);

            Shader.SetGlobalVectorArray(LightPosId, _positions);
            Shader.SetGlobalVectorArray(LightColorId, _colors);
            Shader.SetGlobalVector(ParamsId, new Vector4(count, _brightnessSteps, 0f, 0f));
        }

        // 순간 조명을 먼저 넣고, 남는 칸에 카메라와 가까운 점광을 채운다
        private int CollectLights(Vector3 origin)
        {
            int count = 0;
            float delta = Application.isPlaying ? Time.deltaTime : 0f;

            for (int i = 0; i < MaxFlashes && count < MaxLights; i++)
            {
                if (FlashRemaining[i] <= 0f)
                {
                    continue;
                }

                FlashRemaining[i] -= delta;
                float fade = Mathf.Clamp01(FlashRemaining[i] / FlashDuration[i]);
                Vector3 p = FlashPosition[i];
                _positions[count] = new Vector4(p.x, p.y, p.z, FlashRange[i]);
                Color c = FlashColor[i] * fade;
                _colors[count] = new Vector4(c.r, c.g, c.b, 0f);
                _scores[count] = -1f;
                count++;
            }

            int flashCount = count;
            var lights = ArenaLight.All;

            for (int i = 0; i < lights.Count; i++)
            {
                ArenaLight light = lights[i];
                // 사거리 밖이면 어떤 물체도 밝히지 못해서 후보에서 뺀다
                float score = (light.transform.position - origin).magnitude - light.Range;
                if (score > 40f)
                {
                    continue;
                }

                // 후보가 가득 차면 가장 먼 것과 비교해 더 가까울 때만 바꾼다
                int slot = count;
                if (count >= MaxLights)
                {
                    slot = -1;
                    float worst = score;
                    for (int k = flashCount; k < MaxLights; k++)
                    {
                        if (_scores[k] > worst)
                        {
                            worst = _scores[k];
                            slot = k;
                        }
                    }

                    if (slot < 0)
                    {
                        continue;
                    }
                }
                else
                {
                    count++;
                }

                Vector3 p = light.transform.position;
                float intensity = light.CurrentIntensity;
                _positions[slot] = new Vector4(p.x, p.y, p.z, light.Range);
                _colors[slot] = new Vector4(light.Color.r * intensity, light.Color.g * intensity, light.Color.b * intensity, 0f);
                _scores[slot] = score;
            }

            return count;
        }

        private void OnDisable()
        {
            // 이 스크립트가 꺼지면 셰이더가 조명 없이 원래 밝기로 돌아가도록 끈다
            Shader.SetGlobalVector(BaseId, Vector4.zero);
        }
    }
}
