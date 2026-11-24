using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Enemy
{
    // 무리가 플레이어를 링 모양으로 에워싸 대기하다가 같은 박자에 한꺼번에 덮치는 늑대류 이동
    [RequireComponent(typeof(NavMeshAgent))]
    public class PackMovement : MonoBehaviour, IEnemyMovement
    {
        private enum State
        {
            Approach,
            Surround,
            Lunge,
            Melee
        }

        // 에워싸던 무리가 한 번에 흩어지지 않도록 이 배수보다 멀어지면 다시 접근부터 한다
        private const float LoseDistanceRatio = 1.5f;
        // 자리에 닿았다고 보는 거리. 정확히 겹치길 기다리면 서로 밀며 서성인다
        private const float ArriveDistance = 0.7f;
        private const float SlotSnapDistance = 3f;

        [SerializeField] private float _moveSpeed = 5f;
        [Tooltip("이 거리 안으로 들어오면 곧장 자기 자리로 가서 에워싼다")]
        [SerializeField] private float _engageDistance = 14f;
        [Tooltip("에워싸 대기하는 링의 반지름")]
        [SerializeField] private float _surroundRadius = 5f;
        [Tooltip("자리가 한 겹(8마리)을 넘으면 바깥 겹이 이만큼씩 더 멀리 선다")]
        [SerializeField] private float _ringSpacing = 1.4f;
        [SerializeField] private float _lungeSpeedMultiplier = 1.8f;
        [Tooltip("덮치는 박자(초). 이 주기마다 덮침 창이 열려 에워싼 무리가 같이 달려든다")]
        [SerializeField] private float _lungePeriod = 4f;
        [SerializeField] private float _lungeWindow = 1.2f;
        [Tooltip("덮치다 닿지 못해도 이 시간이 지나면 일반 추격으로 돌아간다")]
        [SerializeField] private float _lungeMaxSeconds = 2.5f;
        [SerializeField] private float _repathInterval = 0.2f;

        private NavMeshAgent _agent;
        private State _state;
        private float _nextRepathTime;
        private float _lungeEndTime;
        private float _phaseOffset;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _moveSpeed;

            // 방향은 YawBillboard가 정하므로 에이전트가 몸을 돌리면 스프라이트가 흔들린다
            _agent.updateRotation = false;

            // 전부 같은 프레임에 튀어나가면 한 점으로 보여서 박자를 조금씩 어긋나게 한다
            _phaseOffset = Random.Range(0f, 0.6f);
        }

        private void OnEnable()
        {
            SurroundCoordinator.Register(transform);
        }

        private void OnDisable()
        {
            SurroundCoordinator.Unregister(transform);
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

            // 시야가 가려지면 자리를 잡아도 덮칠 수 없어서 어떻게든 길을 찾아 접근한다
            if (attackRange <= 0f)
            {
                SetState(State.Approach);
                Go(target.position);
                return;
            }

            switch (_state)
            {
                case State.Approach:
                    if (distance <= _engageDistance)
                    {
                        SetState(State.Surround);
                        break;
                    }

                    Go(target.position);
                    break;

                case State.Surround:
                    if (distance > _engageDistance * LoseDistanceRatio)
                    {
                        SetState(State.Approach);
                    }
                    else if (IsLungeWindow())
                    {
                        _lungeEndTime = Time.time + _lungeMaxSeconds;
                        SetState(State.Lunge);
                    }
                    else
                    {
                        HoldSlot(target, _surroundRadius);
                    }

                    break;

                case State.Lunge:
                    if (distance <= attackRange + 0.3f || Time.time >= _lungeEndTime)
                    {
                        SetState(State.Melee);
                        break;
                    }

                    Go(target.position);
                    break;

                case State.Melee:
                    if (distance > _engageDistance * LoseDistanceRatio)
                    {
                        SetState(State.Approach);
                        break;
                    }

                    // 닿은 뒤에도 서로 다른 각도에 서야 한쪽으로 뭉치지 않는다
                    HoldSlot(target, attackRange * 0.9f);
                    break;
            }
        }

        private void SetState(State state)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;
            _agent.speed = state == State.Lunge ? _moveSpeed * _lungeSpeedMultiplier : _moveSpeed;
        }

        private bool IsLungeWindow()
        {
            return (Time.time + _phaseOffset) % _lungePeriod < _lungeWindow;
        }

        // 곡선 없이 자기 자리를 향해 곧장 간다. 닿으면 멈춰서 기다린다
        private void HoldSlot(Transform target, float baseRadius)
        {
            Vector3 slot = GetSlotPoint(target, baseRadius);
            Vector3 toSlot = slot - transform.position;
            toSlot.y = 0f;

            if (toSlot.magnitude <= ArriveDistance)
            {
                _agent.isStopped = true;
                return;
            }

            Go(slot);
        }

        private Vector3 GetSlotPoint(Transform target, float baseRadius)
        {
            if (!SurroundCoordinator.TryGetSlot(transform, target, out float angle, out int ring))
            {
                return target.position;
            }

            float radius = baseRadius + ring * _ringSpacing;
            Vector3 point = target.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            // 자리가 벽이나 바위 속이면 가장 가까운 바닥으로 당겨 오고, 없으면 플레이어 쪽으로 접근한다
            return NavMesh.SamplePosition(point, out NavMeshHit hit, SlotSnapDistance, NavMesh.AllAreas)
                ? hit.position
                : target.position;
        }

        private void Go(Vector3 destination)
        {
            _agent.isStopped = false;

            if (Time.time >= _nextRepathTime)
            {
                _nextRepathTime = Time.time + _repathInterval;
                _agent.SetDestination(destination);
            }
        }
    }
}
