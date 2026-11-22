using System;
using System.Collections.Generic;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class MinimapHUD : MonoBehaviour
    {
        private class Marker
        {
            public Health Health;
            public Image Icon;
            public Action OnDied;
            public bool IsBig;
            public int Shape = -1;
        }

        private const int ShapeInside = 0;
        private const int ShapeEdge = 1;

        [SerializeField] private WaveSpawner _spawner;
        [SerializeField] private Transform _player;
        [Tooltip("적 아이콘이 놓이는 원형 영역. Mask가 붙은 오브젝트의 자식이어야 가장자리에서 잘린다")]
        [SerializeField] private RectTransform _iconRoot;
        [SerializeField] private Sprite _enemySprite;
        [SerializeField] private Sprite _bigEnemySprite;
        [SerializeField] private Sprite _edgeSprite;
        [Tooltip("미니맵 가장자리까지가 월드에서 몇 미터인지")]
        [SerializeField] private float _worldRadius = 40f;
        [Tooltip("미니맵 중심에서 가장자리 아이콘까지의 거리(화면 단위)")]
        [SerializeField] private float _iconRadius = 126f;
        [Tooltip("체력이 이 값 이상이면 보스로 보고 큰 아이콘을 쓴다")]
        [SerializeField] private int _bigEnemyMinHealth = 10;
        [Tooltip("원본 그림 한 픽셀을 화면 몇 칸으로 그릴지. 다른 HUD와 같게 맞춘다")]
        [SerializeField] private float _pixelScale = 3f;

        private readonly List<Marker> _markers = new List<Marker>();
        // 적이 죽고 생길 때마다 아이콘을 만들고 지우지 않도록 꺼 둔 아이콘을 재사용한다
        private readonly Stack<Image> _pool = new Stack<Image>();

        private void OnEnable()
        {
            if (_spawner != null)
            {
                _spawner.EnemySpawned += OnEnemySpawned;
            }
        }

        private void OnDisable()
        {
            if (_spawner != null)
            {
                _spawner.EnemySpawned -= OnEnemySpawned;
            }

            for (int i = _markers.Count - 1; i >= 0; i--)
            {
                Remove(_markers[i]);
            }
        }

        private void OnEnemySpawned(Health health)
        {
            Image icon = _pool.Count > 0 ? _pool.Pop() : CreateIcon();
            icon.gameObject.SetActive(true);

            var marker = new Marker
            {
                Health = health,
                Icon = icon,
                IsBig = health.Max >= _bigEnemyMinHealth,
            };
            marker.OnDied = () => Remove(marker);

            health.Died += marker.OnDied;
            _markers.Add(marker);
        }

        private Image CreateIcon()
        {
            var go = new GameObject("EnemyIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_iconRoot, false);

            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private void Remove(Marker marker)
        {
            if (marker.Health != null)
            {
                marker.Health.Died -= marker.OnDied;
            }

            marker.Icon.gameObject.SetActive(false);
            _pool.Push(marker.Icon);
            _markers.Remove(marker);
        }

        // 플레이어와 적이 움직인 뒤의 위치로 그려야 같은 프레임에 어긋나지 않는다
        private void LateUpdate()
        {
            if (_player == null)
            {
                return;
            }

            // 플레이어 몸이 시점 yaw를 따라 돌기 때문에 이 각도만큼 되돌리면 바라보는 방향이 위가 된다
            Quaternion toView = Quaternion.Euler(0f, -_player.eulerAngles.y, 0f);

            for (int i = _markers.Count - 1; i >= 0; i--)
            {
                Marker marker = _markers[i];

                if (marker.Health == null)
                {
                    Remove(marker);
                    continue;
                }

                Vector3 offset = toView * (marker.Health.transform.position - _player.position);
                Place(marker, new Vector2(offset.x, offset.z));
            }
        }

        private void Place(Marker marker, Vector2 offset)
        {
            float distance = offset.magnitude;
            bool outside = distance > _worldRadius;
            RectTransform rect = (RectTransform)marker.Icon.transform;

            Vector2 position = outside
                ? offset / distance * _iconRadius
                : offset / _worldRadius * _iconRadius;
            rect.anchoredPosition = position;

            int shape = outside ? ShapeEdge : ShapeInside;
            if (marker.Shape != shape)
            {
                marker.Shape = shape;
                Sprite sprite = outside ? _edgeSprite : (marker.IsBig ? _bigEnemySprite : _enemySprite);
                marker.Icon.sprite = sprite;
                rect.sizeDelta = sprite.rect.size * _pixelScale;
            }

            // 가장자리 화살표는 그림이 위쪽을 향하므로 적이 있는 방향으로 돌린다
            float angle = outside ? Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg - 90f : 0f;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
