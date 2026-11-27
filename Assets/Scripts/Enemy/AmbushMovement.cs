using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Enemy
{
    // 숨어 있다가 가까이 오면 튀어나오는 매복형 이동. 땅에 숨는 전용 모션이 없어 숨은 동안은 그림과 충돌을 끈다
    [RequireComponent(typeof(NavMeshAgent))]
    public class AmbushMovement : MonoBehaviour, IEnemyMovement
    {
        [SerializeField] private float _moveSpeed = 1.6f;
        [SerializeField] private float _emergeDistance = 7f;
        // 숨은 채 아무도 가까이 오지 않으면 웨이브가 영영 안 끝나서 이 시간이 지나면 스스로 나온다
        [SerializeField] private float _maxDormantSeconds = 8f;
        [SerializeField] private float _emergeSeconds = 0.25f;
        [SerializeField] private float _repathInterval = 0.25f;
        [Tooltip("나온 뒤 이 거리 안으로 들어오면 곧장 자기 자리로 가서 다른 근접 적과 함께 에워싼다")]
        [SerializeField] private float _surroundEngageDistance = 12f;
        [Tooltip("자리가 한 겹(8마리)을 넘으면 바깥 겹이 이만큼씩 더 멀리 선다")]
        [SerializeField] private float _ringSpacing = 1.2f;

        // 튀어나오는 연출은 작게 시작해서 키우는 정도라 시작 크기 비율만 둔다
        private const float EmergeStartScale = 0.2f;

        private NavMeshAgent _agent;
        private Collider _collider;
        private SpriteRenderer[] _renderers;
        private Transform _visual;
        private Vector3 _visualScale;
        private float _spawnTime;
        private float _emergeStartTime = -1f;
        private float _nextRepathTime;
        private bool _dormant = true;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _moveSpeed;

            // 방향은 YawBillboard가 정하므로 에이전트가 몸을 돌리면 스프라이트가 흔들린다
            _agent.updateRotation = false;

            _collider = GetComponent<Collider>();
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);

            if (_renderers.Length > 0)
            {
                _visual = _renderers[0].transform;
                _visualScale = _visual.localScale;
            }

            SetHidden(true);
        }

        private void OnEnable()
        {
            _spawnTime = Time.time;
        }

        private void OnDisable()
        {
            SurroundCoordinator.Unregister(transform);
        }

        private void Update()
        {
            if (_emergeStartTime < 0f || _visual == null)
            {
                return;
            }

            float progress = Mathf.Clamp01((Time.time - _emergeStartTime) / _emergeSeconds);
            _visual.localScale = _visualScale * Mathf.Lerp(EmergeStartScale, 1f, progress);

            if (progress >= 1f)
            {
                _emergeStartTime = -1f;
            }
        }

        public void Tick(Transform target, float attackRange)
        {
            // 스폰 위치가 NavMesh 밖이면 에이전트가 만들어지지 않아 명령을 내리면 오류가 난다
            if (!_agent.isOnNavMesh)
            {
                return;
            }

            if (_dormant)
            {
                Vector3 toTarget = target.position - transform.position;
                toTarget.y = 0f;

                bool playerClose = toTarget.magnitude <= _emergeDistance;
                bool waitedLongEnough = Time.time - _spawnTime >= _maxDormantSeconds;

                if (!playerClose && !waitedLongEnough)
                {
                    _agent.isStopped = true;
                    return;
                }

                Emerge();
            }

            _agent.isStopped = false;

            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + _repathInterval;
                _agent.SetDestination(GetDestination(target, attackRange));
            }
        }

        // 가까우면 자기 자리로 곧장 가서 다른 근접 적과 함께 에워싼다
        private Vector3 GetDestination(Transform target, float attackRange)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            if (attackRange > 0f
                && toTarget.magnitude <= _surroundEngageDistance
                && SurroundCoordinator.TryGetSlot(transform, target, out float angle, out int ring))
            {
                float radius = attackRange * 0.9f + ring * _ringSpacing;
                Vector3 slot = target.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

                if (NavMesh.SamplePosition(slot, out NavMeshHit hit, _ringSpacing + 2f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            return target.position;
        }

        private void Emerge()
        {
            _dormant = false;

            // 숨어 있는 동안은 자리를 차지하지 않고, 나온 뒤에만 다른 근접 적과 자리를 나눈다
            SurroundCoordinator.Register(transform);
            SetHidden(false);
            _emergeStartTime = Time.time;

            if (_visual != null)
            {
                _visual.localScale = _visualScale * EmergeStartScale;
            }
        }

        private void SetHidden(bool hidden)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].enabled = !hidden;
            }

            if (_collider != null)
            {
                _collider.enabled = !hidden;
            }
        }
    }
}
