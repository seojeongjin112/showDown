using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 표시만 담당한다. 언제 켜고 끌지는 공격 컴포넌트가 정한다.
    // 총구에서 목표로 흐르는 점선이 한 점으로 가늘어지고, 총구에 빛이 모이다가 마지막에 번쩍여서 "곧 쏜다"가 읽힌다
    [RequireComponent(typeof(LineRenderer))]
    public class AimLaser : MonoBehaviour
    {
        [Header("색")]
        [SerializeField] private Color _startColor = new Color(1f, 0.75f, 0.25f, 0.35f);
        [SerializeField] private Color _endColor = new Color(1f, 0.1f, 0.05f, 1f);
        [SerializeField] private Color _flashColor = new Color(1f, 0.95f, 0.8f, 1f);

        [Header("선")]
        [Tooltip("조준이 시작될 때의 굵기. 끝으로 갈수록 가늘어져 한 점으로 모이는 느낌을 준다")]
        [SerializeField] private float _startWidth = 0.16f;
        [SerializeField] private float _endWidth = 0.04f;
        [Tooltip("점선 한 칸의 길이(m)")]
        [SerializeField] private float _dashLength = 0.6f;
        [Tooltip("점선이 총구에서 목표로 흘러가는 속도(칸/초)")]
        [SerializeField] private float _scrollSpeed = 3f;

        [Header("마지막 번쩍임")]
        [Tooltip("진행이 이 값을 넘으면 밝게 깜빡여 곧 발사한다는 걸 알린다")]
        [SerializeField, Range(0f, 1f)] private float _flashStart = 0.8f;
        [SerializeField] private float _flashPerSecond = 12f;

        [Header("총구에 모이는 빛")]
        [SerializeField] private Sprite _flareSprite;
        [SerializeField] private float _flareMinSize = 0.15f;
        [SerializeField] private float _flareMaxSize = 0.7f;

        private LineRenderer _line;
        private Material _material;
        private SpriteRenderer _flare;
        private Transform _camera;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.useWorldSpace = true;
            _line.textureMode = LineTextureMode.Tile;
            _line.enabled = false;

            // 점선을 흘리려면 재질마다 오프셋이 따로 있어야 해서 이 적만의 사본을 쓴다
            _material = _line.material;

            Camera main = Camera.main;
            _camera = main != null ? main.transform : null;

            if (_flareSprite != null)
            {
                var flareObject = new GameObject("AimFlare");
                flareObject.transform.SetParent(transform, false);
                _flare = flareObject.AddComponent<SpriteRenderer>();
                _flare.sprite = _flareSprite;
                // 적 그림(1)보다 위에 그려야 총구에 모이는 빛이 가려지지 않는다
                _flare.sortingOrder = 3;
                _flare.enabled = false;
            }
        }

        public void Show(Vector3 from, Vector3 to, float progress)
        {
            float t = Mathf.Clamp01(progress);
            Color color = Color.Lerp(_startColor, _endColor, t);

            // 발사 직전에는 밝은 색과 번갈아 깜빡인다
            if (t >= _flashStart && (int)(Time.time * _flashPerSecond * 2f) % 2 == 0)
            {
                color = _flashColor;
            }

            float width = Mathf.Lerp(_startWidth, _endWidth, t);
            _line.startWidth = width;
            _line.endWidth = width;
            _line.startColor = color;
            _line.endColor = color;
            _line.SetPosition(0, from);
            _line.SetPosition(1, to);

            // 타일 모드의 가로 반복은 선 길이를 따라가므로 점선 한 칸이 일정한 길이가 되도록 맞춘다
            float length = Vector3.Distance(from, to);
            _material.mainTextureScale = new Vector2(length / _dashLength, 1f);
            _material.mainTextureOffset = new Vector2(-Time.time * _scrollSpeed, 0f);
            _line.enabled = true;

            ShowFlare(from, color, t);
        }

        public void Hide()
        {
            _line.enabled = false;

            if (_flare != null)
            {
                _flare.enabled = false;
            }
        }

        private void ShowFlare(Vector3 from, Color color, float t)
        {
            if (_flare == null)
            {
                return;
            }

            float size = Mathf.Lerp(_flareMinSize, _flareMaxSize, t);
            _flare.transform.position = from;
            _flare.transform.localScale = Vector3.one * (size / _flare.sprite.bounds.size.x);

            if (_camera != null)
            {
                _flare.transform.rotation = _camera.rotation;
            }

            _flare.color = color;
            _flare.enabled = true;
        }
    }
}
