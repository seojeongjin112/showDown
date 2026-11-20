using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class OrbitMovement : MonoBehaviour, IEnemyMovement
    {
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _orbitRadius = 8f;
        [SerializeField] private bool _clockwise = true;

        private NavMeshAgent _agent;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();

            // 이동량은 아래에서 직접 계산해 Move로 넘기므로 에이전트의 자동 경로 이동과 회전은 쓰지 않는다
            _agent.updateRotation = false;
        }

        // 사거리와 무관하게 궤도 반지름을 유지해야 마차가 멈추지 않고 계속 돌며 쏜다
        public void Tick(Transform target, float attackRange)
        {
            if (!_agent.isOnNavMesh)
            {
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            if (distance < Mathf.Epsilon)
            {
                return;
            }

            Vector3 radial = toTarget / distance;
            Vector3 tangent = Vector3.Cross(Vector3.up, radial) * (_clockwise ? 1f : -1f);

            // 반지름에서 벗어난 만큼 안팎으로 보정해 원이 찌그러지지 않게 한다
            float correction = Mathf.Clamp((distance - _orbitRadius) / _orbitRadius, -1f, 1f);
            Vector3 direction = Vector3.ClampMagnitude(tangent + radial * correction, 1f);

            // Move는 NavMesh 경계에서 막히고 미끄러지므로 건물 안으로 들어가지 못한다
            _agent.Move(direction * (_moveSpeed * Time.deltaTime));
        }
    }
}
