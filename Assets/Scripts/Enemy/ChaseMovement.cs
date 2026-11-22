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

        private NavMeshAgent _agent;
        private float _nextRepathTime;
        private bool _strafeClockwise;
        private bool _isStrafing;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _moveSpeed;

            // 같은 방향으로만 돌면 무리가 한 줄로 몰려서 개체마다 방향을 달리한다
            _strafeClockwise = Random.value < 0.5f;

            // 방향은 YawBillboard가 정하므로 에이전트가 몸을 돌리면 스프라이트가 흔들린다
            _agent.updateRotation = false;
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
                _agent.SetDestination(target.position);
            }
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
