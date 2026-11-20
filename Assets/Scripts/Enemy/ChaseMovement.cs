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

        private NavMeshAgent _agent;
        private float _nextRepathTime;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _moveSpeed;

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
                _agent.isStopped = true;
                return;
            }

            _agent.isStopped = false;

            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + _repathInterval;
                _agent.SetDestination(target.position);
            }
        }
    }
}
