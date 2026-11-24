using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class ChaseMovement : MonoBehaviour, IEnemyMovement
    {
        [SerializeField] private float _moveSpeed = 2f;

        // 매 프레임 경로를 다시 구하면 적이 많을 때 비용이 커서 간격을 둔다
        [SerializeField] private float _repathInterval = 0.25f;

        // 켜면 사거리 안에서 멈추지 않고 플레이어 둘레를 옆으로 돌며 쏜다. 꺼진 적은 예전처럼 선다
        [SerializeField] private bool _strafeInRange;

        [Header("접근 경로")]
        [Tooltip("멀리서 플레이어 옆으로 비껴 가는 최대 폭. 개체마다 방향과 크기가 다르게 정해져 무리가 부채꼴로 퍼진다. 0이면 곧장 달려온다")]
        [SerializeField] private float _flankRadius;
        [Tooltip("좌우로 흔들며 달려오는 폭. 0이면 흔들지 않는다")]
        [SerializeField] private float _weaveAmplitude;
        [Tooltip("흔드는 속도(라디안/초). 클수록 지그재그가 촘촘해진다")]
        [SerializeField] private float _weaveFrequency = 3f;
        [Tooltip("이 거리 이상에서는 치우침이 최대다")]
        [SerializeField] private float _flankFullDistance = 30f;
        [Tooltip("이 거리 이하에서는 치우침이 사라져 곧장 들어온다")]
        [SerializeField] private float _flankEndDistance = 6f;

        [Header("에워싸기 (근접 적만)")]
        [Tooltip("켜면 가까이 왔을 때 곡선 없이 곧장 자기 자리로 가서 플레이어 둘레를 고르게 에워싼다. 원거리 적은 끈다")]
        [SerializeField] private bool _surroundWhenClose;
        [Tooltip("이 거리 안으로 들어오면 에워싸기 자리로 간다")]
        [SerializeField] private float _surroundEngageDistance = 14f;
        [Tooltip("자리가 한 겹(8마리)을 넘으면 바깥 겹이 이만큼씩 더 멀리 선다")]
        [SerializeField] private float _ringSpacing = 1.2f;

        private NavMeshAgent _agent;
        private float _nextRepathTime;
        private bool _strafeClockwise;
        private bool _isStrafing;
        private float _flankSide;
        private float _flankScale;
        private float _weavePhase;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _moveSpeed;

            // 같은 방향으로만 돌면 무리가 한 줄로 몰려서 개체마다 방향을 달리한다
            _strafeClockwise = Random.value < 0.5f;

            // 모두 같은 쪽으로 비끼거나 같은 박자로 흔들면 오히려 한 덩어리가 되어 개체마다 따로 정한다
            _flankSide = Random.value < 0.5f ? -1f : 1f;
            _flankScale = Random.Range(0.5f, 1f);
            _weavePhase = Random.Range(0f, Mathf.PI * 2f);

            // 방향은 YawBillboard가 정하므로 에이전트가 몸을 돌리면 스프라이트가 흔들린다
            _agent.updateRotation = false;
        }

        private void OnEnable()
        {
            if (_surroundWhenClose)
            {
                SurroundCoordinator.Register(transform);
            }
        }

        private void OnDisable()
        {
            if (_surroundWhenClose)
            {
                SurroundCoordinator.Unregister(transform);
            }
        }

        public void Tick(Transform target, float attackRange)
        {
            // 스폰 위치가 NavMesh 밖이면 에이전트가 만들어지지 않아 명령을 내리면 오류가 난다
            if (!_agent.isOnNavMesh)
            {
                return;
            }

            // 높이 차이를 무시해야 플레이어 눈높이 쪽으로 적이 떠오르지 않는다
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= attackRange)
            {
                if (_strafeInRange)
                {
                    Strafe(toTarget);
                    return;
                }

                _agent.isStopped = true;
                return;
            }

            _isStrafing = false;
            _agent.isStopped = false;

            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + _repathInterval;
                _agent.SetDestination(GetDestination(target, toTarget, attackRange));
            }
        }

        // 가까우면 곡선을 버리고 자기 자리로 곧장, 멀면 비껴서 접근한다
        private Vector3 GetDestination(Transform target, Vector3 toTarget, float attackRange)
        {
            if (_surroundWhenClose
                && toTarget.magnitude <= _surroundEngageDistance
                && SurroundCoordinator.TryGetSlot(transform, target, out float angle, out int ring))
            {
                float radius = attackRange * 0.9f + ring * _ringSpacing;
                Vector3 slot = target.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

                // 자리가 벽이나 바위 속이면 가장 가까운 바닥으로 당겨 오고, 없으면 평소처럼 접근한다
                if (NavMesh.SamplePosition(slot, out NavMeshHit hit, _ringSpacing + 2f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            return GetApproachPoint(target.position, toTarget);
        }

        // 플레이어 위치 자체가 아니라 그 옆의 한 점을 노려야 직선이 아닌 호를 그리며 다가온다
        private Vector3 GetApproachPoint(Vector3 targetPosition, Vector3 toTarget)
        {
            float distance = toTarget.magnitude;
            if ((_flankRadius <= 0f && _weaveAmplitude <= 0f) || distance <= Mathf.Epsilon)
            {
                return targetPosition;
            }

            // 가까워질수록 치우침을 줄여야 끝내 플레이어에게 닿는다
            float taper = Mathf.InverseLerp(_flankEndDistance, _flankFullDistance, distance);
            if (taper <= 0f)
            {
                return targetPosition;
            }

            Vector3 direction = toTarget / distance;
            Vector3 right = new Vector3(direction.z, 0f, -direction.x);

            float lateral = _flankSide * _flankScale * _flankRadius
                + Mathf.Sin(Time.time * _weaveFrequency + _weavePhase) * _weaveAmplitude;
            Vector3 point = targetPosition + right * (lateral * taper);

            // 좁은 협곡에서는 옆이 벽이라 NavMesh 안쪽 가장 가까운 점으로 당겨 와야 한다
            float searchRadius = _flankRadius + _weaveAmplitude + 1f;
            return NavMesh.SamplePosition(point, out NavMeshHit hit, searchRadius, NavMesh.AllAreas)
                ? hit.position
                : targetPosition;
        }

        private void Strafe(Vector3 toTarget)
        {
            // 남은 경로 이동이 Move와 겹쳐 서로 밀어내므로 옆걸음을 시작할 때 한 번 끊는다
            if (!_isStrafing)
            {
                _isStrafing = true;
                _agent.ResetPath();
            }

            Vector3 radial = toTarget.normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, radial) * (_strafeClockwise ? 1f : -1f);

            // Move는 NavMesh 경계에서 막히고 미끄러지므로 건물 안으로 들어가지 못한다
            _agent.Move(tangent * (_moveSpeed * Time.deltaTime));
        }
    }
}
