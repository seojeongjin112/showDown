using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.AI;

namespace ArenaBreak.Enemy
{
    [RequireComponent(typeof(Health))]
    public class EnemyDeath : MonoBehaviour
    {
        // 사망 애니메이션을 붙일 때 재생 시간만큼 늘리려고 열어 둔다
        [SerializeField] private float _destroyDelay;

        private Health _health;
        private EnemyAI _ai;
        private Collider _collider;
        private NavMeshAgent _agent;
        private Behaviour _attack;

        private void Awake()
        {
            // 공격 구현은 IEnemyAttack이면서 MonoBehaviour라 Behaviour로 받아 끌 수 있다
            _attack = GetComponent<IEnemyAttack>() as Behaviour;
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _ai = GetComponent<EnemyAI>();
            _collider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
        }

        private void OnDied()
        {
            // 지연 동안 시체가 계속 공격하거나 칼에 또 맞지 않도록 먼저 끈다
            // 보스처럼 EnemyAI 없이 자체 AI를 쓰는 적도 있다
            if (_ai != null)
            {
                _ai.enabled = false;
            }

            _collider.enabled = false;

            // AI만 꺼도 조준 중이던 공격은 자체 Update로 계속 진행돼 시체가 레이저를 쏘고 발사까지 한다
            if (_attack != null)
            {
                _attack.enabled = false;
            }

            // AI가 꺼져도 에이전트는 마지막 목적지로 계속 가므로, 사망 연출 동안 시체가 미끄러지지 않게 세운다
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }

            // 적 한 마리 단위의 드문 파괴라 풀링 없이 Destroy로 충분하다
            Destroy(gameObject, _destroyDelay);
        }
    }
}
