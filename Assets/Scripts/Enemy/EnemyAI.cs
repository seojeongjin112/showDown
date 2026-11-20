using UnityEngine;

namespace ArenaBreak.Enemy
{
    public class EnemyAI : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [SerializeField] private Transform _target;

        private IEnemyAttack _attack;
        private IEnemyMovement _movement;

        private void Awake()
        {
            _attack = GetComponent<IEnemyAttack>();
            _movement = GetComponent<IEnemyMovement>();
            if (_attack == null || _movement == null)
            {
                Debug.LogError("EnemyAI: IEnemyAttack과 IEnemyMovement 구현 컴포넌트가 모두 필요합니다", this);
                enabled = false;
                return;
            }

            if (_target == null)
            {
                GameObject player = GameObject.FindWithTag(PlayerTag);
                if (player == null)
                {
                    Debug.LogError("EnemyAI: 타겟이 없고 Player 태그 오브젝트도 찾지 못했습니다", this);
                    enabled = false;
                    return;
                }

                _target = player.transform;
            }
        }

        private void Update()
        {
            _movement.Tick(_target, _attack.Range);

            // 선회하는 적도 사거리 안이면 움직이는 중에 쏴야 하므로 이동 뒤에 따로 판정한다
            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= _attack.Range && _attack.IsReady)
            {
                _attack.Execute(_target);
            }
        }
    }
}
