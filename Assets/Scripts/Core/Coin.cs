using UnityEngine;

namespace ArenaBreak.Core
{
    // 적 처치마다 생성·파괴가 반복되므로 풀링 대상
    public class Coin : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [SerializeField] private int _value = 1;
        [SerializeField] private float _pickupRadius = 1.5f;
        [SerializeField] private float _flightTime = 0.5f;
        // 그림 중심이 바닥에 파묻히지 않도록 중심을 띄워 둔다
        [SerializeField] private float _restHeight = 0.4f;

        private CoinWallet _wallet;
        private Transform _player;
        private Vector3 _velocity;
        private bool _landed;

        private void Awake()
        {
            GameObject player = GameObject.FindWithTag(PlayerTag);
            if (player == null || !player.TryGetComponent(out _wallet))
            {
                Debug.LogError("Coin: Player 태그 오브젝트나 CoinWallet을 찾지 못했습니다", this);
                enabled = false;
                return;
            }

            _player = player.transform;
        }

        // 착지 지점을 먼저 정하고 거기에 정확히 _flightTime 뒤에 닿는 속도를 역산한다
        public void Launch(Vector3 landingPoint)
        {
            Vector3 toLanding = new Vector3(landingPoint.x, _restHeight, landingPoint.z) - transform.position;
            float gravity = Physics.gravity.y;

            _velocity = new Vector3(
                toLanding.x / _flightTime,
                (toLanding.y - 0.5f * gravity * _flightTime * _flightTime) / _flightTime,
                toLanding.z / _flightTime);
            _landed = false;
        }

        private void Update()
        {
            if (!_landed)
            {
                Fall();
                return;
            }

            // 높이 차이를 무시해야 점프 중이거나 낮은 곳에서도 같은 거리로 줍는다
            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude <= _pickupRadius * _pickupRadius)
            {
                _wallet.Add(_value);
                Destroy(gameObject);
            }
        }

        private void Fall()
        {
            _velocity += Physics.gravity * Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;

            if (transform.position.y <= _restHeight && _velocity.y <= 0f)
            {
                Vector3 position = transform.position;
                position.y = _restHeight;
                transform.position = position;
                _landed = true;
            }
        }
    }
}
