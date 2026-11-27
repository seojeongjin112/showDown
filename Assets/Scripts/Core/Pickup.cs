using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Core
{
    // 적 처치마다 생성·파괴가 반복되므로 풀링 대상
    public abstract class Pickup : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [SerializeField] private float _pickupRadius = 1.5f;
        [SerializeField] private float _flightTime = 0.5f;
        // 그림 중심이 바닥에 파묻히지 않도록 중심을 띄워 둔다
        [SerializeField] private float _restHeight = 0.4f;
        // 멀리서도 떨어진 자리를 알아보게 하는 빛 기둥과 바닥링. 비어 있으면 표시하지 않는다
        [SerializeField] private GameObject _highlightPrefab;

        // 종류마다 스크립트는 달라도 떨어지고 줍는 흐름은 여기 하나라서 소리도 여기서 낸다
        [Header("Sound")]
        [Tooltip("바닥에 떨어진 순간. 무기처럼 좋은 아이템만 넣어서 떨어졌다는 걸 알린다")]
        [SerializeField] private AudioClip _landSound;
        [SerializeField] private AudioClip _pickupSound;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.8f;

        private Vector3 _velocity;
        private bool _landed;
        private GameObject _highlight;

        protected Transform Player { get; private set; }

        private Health _playerHealth;

        protected virtual void Awake()
        {
            GameObject player = GameObject.FindWithTag(PlayerTag);
            if (player == null)
            {
                Debug.LogError("Pickup: Player 태그 오브젝트를 찾지 못했습니다", this);
                enabled = false;
                return;
            }

            Player = player.transform;
            _playerHealth = player.GetComponent<Health>();
        }

        // 벽 가까이서 죽어도 떨어진 것이 건물 안에 박혀 못 줍는 일이 없도록 걸을 수 있는 바닥으로 옮긴 착지 지점을 구한다
        public static Vector3 ScatterPoint(Vector3 origin, float radius, float snapDistance)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 landing = origin + new Vector3(offset.x, 0f, offset.y);

            return NavMesh.SamplePosition(landing, out NavMeshHit hit, snapDistance, NavMesh.AllAreas)
                ? hit.position
                : landing;
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

        protected abstract void OnPickedUp();

        // 줍기 전에 확인이 필요한 아이템(무기 교체 등)이 거절할 수 있게 한다. 기본은 닿으면 바로 줍는다
        protected virtual bool TryPickUp()
        {
            OnPickedUp();
            return true;
        }

        // 플레이어가 줍는 거리 밖에 있는 동안 불린다. 확인 중이던 상태를 접는 데 쓴다
        protected virtual void OnOutOfRange()
        {
        }

        private void Update()
        {
            if (!_landed)
            {
                Fall();
                return;
            }

            // 죽은 뒤 쓰러지는 동안 경험치를 주워 레벨업 창이 뜨면, 창을 닫을 때 꺼 둔 조작이 다시 켜져 결과 화면 버튼을 누를 수 없게 된다
            if (_playerHealth != null && _playerHealth.IsDead)
            {
                return;
            }

            // 높이 차이를 무시해야 점프 중이거나 낮은 곳에서도 같은 거리로 줍는다
            Vector3 toPlayer = Player.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude <= _pickupRadius * _pickupRadius)
            {
                if (TryPickUp())
                {
                    // 줍는 사람 귀에서 나는 소리라 2D로 낸다. 무기 교체를 거절하면 주운 게 아니라서 소리도 없다
                    SfxPlayer.Play2D(_pickupSound, _volume);
                    Destroy(gameObject);
                }
            }
            else
            {
                OnOutOfRange();
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
                SpawnHighlight(position);
                SfxPlayer.PlayAt(_landSound, position, _volume);
            }
        }

        // 아이템 중심은 바닥에서 _restHeight만큼 떠 있으므로 효과는 그만큼 내린 바닥에 놓는다
        private void SpawnHighlight(Vector3 restPosition)
        {
            if (_highlightPrefab == null)
            {
                return;
            }

            Vector3 ground = restPosition - Vector3.up * _restHeight;
            _highlight = Instantiate(_highlightPrefab, ground, Quaternion.identity);
        }

        // 주워서든 다른 이유로든 아이템이 사라지면 바닥에 효과만 남지 않게 함께 지운다
        private void OnDestroy()
        {
            if (_highlight != null)
            {
                Destroy(_highlight);
            }
        }
    }
}
