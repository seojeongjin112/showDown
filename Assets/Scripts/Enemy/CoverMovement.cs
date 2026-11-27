using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Enemy
{
    // 쏜 뒤에는 엄폐물 뒤로 숨고, 재장전이 끝나면 시야가 트이는 자리로 나와 쏘는 원거리 이동
    [RequireComponent(typeof(NavMeshAgent))]
    public class CoverMovement : MonoBehaviour, IEnemyMovement
    {
        private enum Mode
        {
            Approach,
            GoToPoint,
            Hold
        }

        // 도착했다고 보는 거리. 정확히 겹치길 기다리면 경계에서 멈춰 서성이게 된다
        private const float ArriveDistance = 0.6f;
        private const float SampleSnapDistance = 2f;

        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _searchRadius = 12f;
        [Tooltip("한 번 판단할 때 살펴보는 후보 지점 수. 많을수록 좋은 자리를 찾지만 시야 판정(레이캐스트)이 늘어난다")]
        [SerializeField] private int _sampleCount = 14;
        [Tooltip("자리를 다시 고르는 간격. 매 프레임 고르면 숨고 나오기를 떨며 반복한다")]
        [SerializeField] private float _decisionInterval = 0.6f;
        // EnemyAI와 같은 높이로 시야를 봐야 숨었다고 본 자리에서 실제로도 가려진다
        [SerializeField] private float _eyeHeight = 1f;
        [SerializeField] private float _targetHeight = 1.5f;
        [SerializeField] private float _repathInterval = 0.25f;

        private NavMeshAgent _agent;
        private IEnemyAttack _attack;
        private Mode _mode;
        private Vector3 _point;
        private float _nextDecisionTime;
        private float _nextRepathTime;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _moveSpeed;

            // 방향은 YawBillboard가 정하므로 에이전트가 몸을 돌리면 스프라이트가 흔들린다
            _agent.updateRotation = false;

            _attack = GetComponent<IEnemyAttack>();
            if (_attack == null)
            {
                Debug.LogError("CoverMovement: IEnemyAttack 구현이 필요하다.", this);
                enabled = false;
            }
        }

        public void Tick(Transform target, float attackRange)
        {
            // 스폰 위치가 NavMesh 밖이면 에이전트가 만들어지지 않아 명령을 내리면 오류가 난다
            if (!_agent.isOnNavMesh)
            {
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            // attackRange가 0이면 EnemyAI가 시야가 가려졌다고 알린 것이다
            bool exposed = attackRange > 0f;
            bool ready = _attack.IsReady;

            // 쏠 수 있는 자리에서 준비까지 됐으면 멈춰서 쏜다
            if (exposed && ready && distance <= attackRange)
            {
                _agent.isStopped = true;
                _mode = Mode.Hold;
                _nextDecisionTime = Time.time + _decisionInterval;
                return;
            }

            if (Time.time >= _nextDecisionTime)
            {
                _nextDecisionTime = Time.time + _decisionInterval;
                Decide(target, exposed, ready, attackRange);
            }

            Move(target);
        }

        private void Decide(Transform target, bool exposed, bool ready, float attackRange)
        {
            if (ready)
            {
                // 시야가 트이지 않았으면 쏠 수 있는 자리를 찾고, 없으면 길을 따라 접근한다
                if (!exposed && TryFindPoint(target, false, attackRange > 0f ? attackRange : float.MaxValue, out _point))
                {
                    _mode = Mode.GoToPoint;
                    return;
                }

                _mode = Mode.Approach;
                return;
            }

            // 재장전 중에는 노출돼 있으면 숨을 자리를 찾고, 이미 가려져 있으면 그대로 기다린다
            if (!exposed)
            {
                _mode = Mode.Hold;
                return;
            }

            _mode = TryFindPoint(target, true, float.MaxValue, out _point) ? Mode.GoToPoint : Mode.Hold;
        }

        private void Move(Transform target)
        {
            switch (_mode)
            {
                case Mode.Hold:
                    _agent.isStopped = true;
                    break;

                case Mode.Approach:
                    _agent.isStopped = false;
                    Repath(target.position);
                    break;

                case Mode.GoToPoint:
                    Vector3 toPoint = _point - transform.position;
                    toPoint.y = 0f;

                    if (toPoint.magnitude <= ArriveDistance)
                    {
                        _agent.isStopped = true;
                        break;
                    }

                    _agent.isStopped = false;
                    Repath(_point);
                    break;
            }
        }

        private void Repath(Vector3 destination)
        {
            if (Time.time < _nextRepathTime)
            {
                return;
            }

            _nextRepathTime = Time.time + _repathInterval;
            _agent.SetDestination(destination);
        }

        // wantBlocked가 참이면 플레이어 시야에서 가려진 자리, 거짓이면 사거리 안에서 시야가 트인 자리를 찾는다
        private bool TryFindPoint(Transform target, bool wantBlocked, float maxRange, out Vector3 result)
        {
            result = transform.position;
            float bestDistance = float.MaxValue;
            bool found = false;
            Vector3 targetEye = target.position + Vector3.up * _targetHeight;

            for (int i = 0; i < _sampleCount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * _searchRadius;
                Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, SampleSnapDistance, NavMesh.AllAreas))
                {
                    continue;
                }

                Vector3 position = hit.position;
                Vector3 toTarget = target.position - position;
                toTarget.y = 0f;

                if (!wantBlocked && toTarget.magnitude > maxRange)
                {
                    continue;
                }

                bool blocked = ProjectileObstacle.IsBlocked(position + Vector3.up * _eyeHeight, targetEye);
                if (blocked != wantBlocked)
                {
                    continue;
                }

                // 가까운 자리일수록 빨리 숨거나 나올 수 있어 가장 가까운 후보를 고른다
                Vector3 fromSelf = position - transform.position;
                fromSelf.y = 0f;
                float distance = fromSelf.magnitude;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    result = position;
                    found = true;
                }
            }

            return found;
        }
    }
}
