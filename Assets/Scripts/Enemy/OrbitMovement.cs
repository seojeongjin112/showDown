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
        [SerializeField] private float _repathInterval = 0.25f;

        private NavMeshAgent _agent;
        private float _nextRepathTime;
        private bool _isPathing;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _moveSpeed;

            // 궤도 이동은 아래에서 직접 계산해 Move로 넘기고, 경로 이동은 에이전트에 맡기므로 회전은 쓰지 않는다
            _agent.updateRotation = false;
        }

        // 사거리와 무관하게 궤도 반지름을 유지해야 마차가 멈추지 않고 계속 돌며 쏜다
        public void Tick(Transform target, float attackRange)
        {
            if (!_agent.isOnNavMesh)
            {
                return;
            }

            // 시야가 가려져 사거리가 0으로 오면 궤도를 돌아도 쏠 수 없어서 길을 찾아 접근한다
            if (attackRange <= 0f)
            {
                ApproachByPath(target);
                return;
            }

            // 경로 이동이 남아 있으면 Move와 겹쳐 서로 밀어내므로 궤도로 돌아오기 전에 끊는다
            if (_isPathing)
            {
                _agent.ResetPath();
                _isPathing = false;
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

        private void ApproachByPath(Transform target)
        {
            _isPathing = true;

            // 매 프레임 경로를 다시 구하면 적이 많을 때 비용이 커서 간격을 둔다
            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + _repathInterval;
                _agent.SetDestination(target.position);
            }
        }
    }
}
