using UnityEngine;

namespace ArenaBreak.UI
{
    public class TitleTumbleweed : MonoBehaviour
    {
        [SerializeField] private float _speed = 260f;
        [SerializeField] private float _groundY = 52f;
        [SerializeField] private float _bounceHeight = 28f;
        [SerializeField] private float _bouncesPerSecond = 1.6f;
        [SerializeField] private float _startDelay;
        [SerializeField] private Vector2 _restartDelayRange = new Vector2(1.5f, 4f);

        private RectTransform _rect;
        private RectTransform _area;
        private float _radius;
        private float _x;
        private float _rollTime;
        private float _waitTimer;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _area = (RectTransform)_rect.parent;
            _radius = _rect.rect.width * 0.5f;
        }

        private void OnEnable()
        {
            _waitTimer = _startDelay;
            ResetToStart();
        }

        private void Update()
        {
            if (_waitTimer > 0f)
            {
                _waitTimer -= Time.deltaTime;
                return;
            }

            _x += _speed * Time.deltaTime;
            _rollTime += Time.deltaTime;

            float bounce = Mathf.Abs(Mathf.Sin(_rollTime * _bouncesPerSecond * Mathf.PI)) * _bounceHeight;
            _rect.anchoredPosition = new Vector2(_x, _groundY + bounce);
            // 이동 거리만큼 회전시켜야 미끄러지지 않고 굴러가는 것처럼 보인다
            _rect.localRotation = Quaternion.Euler(0f, 0f, -(_x / _radius) * Mathf.Rad2Deg);

            if (_x > _area.rect.width + _radius)
            {
                ResetToStart();
                _waitTimer = Random.Range(_restartDelayRange.x, _restartDelayRange.y);
            }
        }

        private void ResetToStart()
        {
            _x = -_radius;
            _rollTime = 0f;
            _rect.anchoredPosition = new Vector2(_x, _groundY);
        }
    }
}
