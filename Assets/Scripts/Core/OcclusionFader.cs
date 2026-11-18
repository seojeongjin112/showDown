using System.Collections.Generic;
using UnityEngine;

namespace ArenaBreak.Core
{
    public class OcclusionFader : MonoBehaviour
    {
        // URP 블렌드 값. 알파 블렌드는 (SrcAlpha, OneMinusSrcAlpha)
        private const float BlendSrcAlpha = 5f;
        private const float BlendOneMinusSrcAlpha = 10f;
        private const float BlendOne = 1f;
        private const int TransparentQueue = 3000;
        // 알파 컷오프와 투명도가 같은 알파를 쓰므로, 0.5로 두면 반투명해지는 순간 전부 잘려 나간다
        private const float FadeCutoff = 0.01f;
        private const float OpaqueThreshold = 0.999f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Transform _target;
        [SerializeField] private Camera _camera;
        // 루트 하나가 건물 하나다. 아래의 모든 렌더러가 함께 페이드되고, 가림 판정도 전체 범위로 한다
        [SerializeField] private Transform[] _fadeRoots;
        [SerializeField, Range(0f, 1f)] private float _occludedAlpha = 0.2f;
        [SerializeField] private float _fadeSpeed = 8f;
        // 발이 아니라 몸통이 가려지는지를 본다
        [SerializeField] private float _targetHeight = 1f;
        // 건물 화면 사각형 경계에서 깜빡이지 않도록 둔 여유 픽셀
        [SerializeField] private float _screenPadding = 8f;
        // 같은 깊이로 붙어 있는 건물이 서로 가린다고 판정되지 않도록 하는 여유 거리
        [SerializeField] private float _depthMargin = 0.1f;

        private readonly List<FadeGroup> _groups = new List<FadeGroup>();

        // 불투명 상태에서는 깊이가 정확하고, 가릴 때만 투명 복제 머티리얼로 바꿔 끼운다
        private sealed class FadeGroup
        {
            public readonly List<Renderer> Renderers = new List<Renderer>();
            public readonly List<SpriteRenderer> Sprites = new List<SpriteRenderer>();
            public readonly List<MeshRenderer> Meshes = new List<MeshRenderer>();
            public readonly List<Material[]> Originals = new List<Material[]>();
            public readonly List<Material[]> Fades = new List<Material[]>();
            public readonly List<Material> OwnedMaterials = new List<Material>();
            public float Alpha = 1f;
            public bool UsingFade;
        }

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            foreach (Transform root in _fadeRoots)
            {
                if (root != null)
                {
                    _groups.Add(BuildGroup(root));
                }
            }
        }

        private void OnDestroy()
        {
            foreach (FadeGroup group in _groups)
            {
                foreach (Material material in group.OwnedMaterials)
                {
                    Destroy(material);
                }
            }
        }

        private FadeGroup BuildGroup(Transform root)
        {
            var group = new FadeGroup();
            var fadeBySource = new Dictionary<Material, Material>();

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                group.Renderers.Add(renderer);

                var sprite = renderer as SpriteRenderer;
                if (sprite != null)
                {
                    group.Sprites.Add(sprite);
                    continue;
                }

                var mesh = renderer as MeshRenderer;
                if (mesh == null)
                {
                    continue;
                }

                Material[] originals = mesh.sharedMaterials;
                var fades = new Material[originals.Length];
                for (int i = 0; i < originals.Length; i++)
                {
                    if (!fadeBySource.TryGetValue(originals[i], out Material fade))
                    {
                        fade = CreateFadeMaterial(originals[i]);
                        fadeBySource.Add(originals[i], fade);
                        group.OwnedMaterials.Add(fade);
                    }

                    fades[i] = fade;
                }

                group.Meshes.Add(mesh);
                group.Originals.Add(originals);
                group.Fades.Add(fades);
            }

            return group;
        }

        private static Material CreateFadeMaterial(Material source)
        {
            var fade = new Material(source);
            fade.SetFloat("_Surface", 1f);
            fade.SetFloat("_Blend", 0f);
            fade.SetFloat("_SrcBlend", BlendSrcAlpha);
            fade.SetFloat("_DstBlend", BlendOneMinusSrcAlpha);
            fade.SetFloat("_SrcBlendAlpha", BlendOne);
            fade.SetFloat("_DstBlendAlpha", BlendOneMinusSrcAlpha);
            fade.SetFloat("_ZWrite", 0f);
            fade.SetFloat("_Cutoff", FadeCutoff);
            fade.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            fade.SetOverrideTag("RenderType", "Transparent");
            fade.renderQueue = TransparentQueue;
            return fade;
        }

        // 카메라와 Billboard가 같은 프레임에 움직인 뒤에 판정해야 한다
        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 playerScreen = _camera.WorldToScreenPoint(_target.position + Vector3.up * _targetHeight);

            foreach (FadeGroup group in _groups)
            {
                float goal = IsOccluding(group, playerScreen) ? _occludedAlpha : 1f;
                group.Alpha = Mathf.MoveTowards(group.Alpha, goal, _fadeSpeed * Time.deltaTime);
                Apply(group);
            }
        }

        private void Apply(FadeGroup group)
        {
            foreach (SpriteRenderer sprite in group.Sprites)
            {
                Color color = sprite.color;
                if (!Mathf.Approximately(color.a, group.Alpha))
                {
                    color.a = group.Alpha;
                    sprite.color = color;
                }
            }

            if (group.Meshes.Count == 0)
            {
                return;
            }

            if (group.Alpha >= OpaqueThreshold)
            {
                if (group.UsingFade)
                {
                    for (int i = 0; i < group.Meshes.Count; i++)
                    {
                        group.Meshes[i].sharedMaterials = group.Originals[i];
                    }

                    group.UsingFade = false;
                }

                return;
            }

            if (!group.UsingFade)
            {
                for (int i = 0; i < group.Meshes.Count; i++)
                {
                    group.Meshes[i].sharedMaterials = group.Fades[i];
                }

                group.UsingFade = true;
            }

            foreach (Material material in group.OwnedMaterials)
            {
                Color color = material.GetColor(BaseColorId);
                color.a = group.Alpha;
                material.SetColor(BaseColorId, color);
            }
        }

        // 건물의 가장 가까운 면이 플레이어보다 카메라에 가깝고, 플레이어의 화면 위치가 건물 전체의 화면 영역 안에 있을 때만 가린다고 본다
        private bool IsOccluding(FadeGroup group, Vector3 playerScreen)
        {
            if (playerScreen.z <= 0f)
            {
                return false;
            }

            Bounds bounds = default;
            bool hasBounds = false;
            foreach (Renderer renderer in group.Renderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                return false;
            }

            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            float left = float.MaxValue;
            float right = float.MinValue;
            float bottom = float.MaxValue;
            float top = float.MinValue;
            float nearestDepth = float.MaxValue;

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z);
                Vector3 screen = _camera.WorldToScreenPoint(point);
                if (screen.z <= 0f)
                {
                    continue;
                }

                left = Mathf.Min(left, screen.x);
                right = Mathf.Max(right, screen.x);
                bottom = Mathf.Min(bottom, screen.y);
                top = Mathf.Max(top, screen.y);
                nearestDepth = Mathf.Min(nearestDepth, screen.z);
            }

            if (nearestDepth >= playerScreen.z - _depthMargin)
            {
                return false;
            }

            return playerScreen.x >= left - _screenPadding && playerScreen.x <= right + _screenPadding
                && playerScreen.y >= bottom - _screenPadding && playerScreen.y <= top + _screenPadding;
        }
    }
}
