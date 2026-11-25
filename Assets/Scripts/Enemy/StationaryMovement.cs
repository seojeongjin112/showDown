using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Enemy
{
    // 쓰러진 마차처럼 제자리에서 쏘는 단계도 EnemyAI는 이동 구현을 요구하므로, 아무것도 하지 않는 이동을 끼운다
    public class StationaryMovement : MonoBehaviour, IEnemyMovement
    {
        private NavMeshAgent _agent;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        public void Tick(Transform target, float attackRange)
        {
            // 에이전트가 달려 있으면 이전 목적지로 계속 미끄러지지 않도록 세워 둔다
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }
        }
    }
}
