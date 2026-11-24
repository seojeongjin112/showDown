using UnityEngine;

namespace ArenaBreak.Core
{
    // 폭탄이 떨어질 자리에 폭발 반경을 바닥 원으로 보여 줘서 플레이어가 피할 곳을 알게 한다
    public class BombBlastMarker : MonoBehaviour
    {
        private const int TextureSize = 128;
        private const float RingWidth = 0.1f;
        private const float FillAlpha = 0.4f;
        // 바닥 메시와 겹쳐 깜빡이지 않게 살짝 띄운다
        private const float GroundLift = 0.05f;

        private static Sprite s_sprite;

        [SerializeField] private Color _farColor = new Color(1f, 0.4f, 0.1f, 0.8f);
        [SerializeField] private Color _nearColor = new Color(1f, 0.1f, 0.05f, 1f);
        [SerializeField] private float _pulseSpeed = 10f;

        private SpriteRenderer _renderer;
        private float _duration;
        private float _elapsed;

        public static BombBlastMarker Spawn(Material material, Vector3 groundPoint, float radius, float duration)
        {
            GameObject marker = new GameObject("BombBlastMarker");
            marker.transform.position = groundPoint + Vector3.up * GroundLift;
            // 눕혀서 바닥에 붙인다. 스프라이트 지름이 1유닛이 되도록 만들었으니 반경의 2배가 크기다
            marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            marker.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

            SpriteRenderer sprite = marker.AddComponent<SpriteRenderer>();
            sprite.sprite = GetSprite();
            sprite.sharedMaterial = material;

            BombBlastMarker result = marker.AddComponent<BombBlastMarker>();
            result._renderer = sprite;
            result._duration = Mathf.Max(duration, 0.01f);
            return result;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / _duration);

            // 떨어질 때가 가까울수록 붉고 빠르게 깜빡여 곧 터진다는 걸 알린다
            Color color = Color.Lerp(_farColor, _nearColor, progress);
            float pulse = 0.75f + 0.25f * Mathf.Sin(_elapsed * _pulseSpeed * (1f + progress * 2f));
            color.a *= pulse;
            _renderer.color = color;

            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
            }
        }

        // 한 번만 만들어 모든 폭탄이 공유한다
        private static Sprite GetSprite()
        {
            if (s_sprite != null)
            {
                return s_sprite;
            }

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float center = (TextureSize - 1) * 0.5f;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / (TextureSize * 0.5f);
                    float alpha = 0f;
                    if (distance <= 1f)
                    {
                        alpha = distance >= 1f - RingWidth ? 1f : FillAlpha;
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            s_sprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
            return s_sprite;
        }
    }
}
