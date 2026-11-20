using UnityEngine;

namespace ArenaBreak.Enemy
{
    public class EnemyAI : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [SerializeField] private Transform _target;
        [SerializeField] private float _eyeHeight = 1f;
        // 플레이어 루트는 발밑이라 가슴 높이를 보아야 낮은 장애물 너머의 플레이어를 가리지 않는다
        [SerializeField] private float _sightTargetHeight = 1.5f;

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
            Vector3 eye = transform.position + Vector3.up * _eyeHeight;
            Vector3 sightPoint = _target.position + Vector3.up * _sightTargetHeight;
            bool canSee = !ProjectileObstacle.IsBlocked(eye, sightPoint);

            // 건물에 가려졌는데 사거리 안이라고 멈추면 벽만 쏘게 되므로, 사거리를 0으로 넘겨 돌아서라도 계속 쫓게 한다
            _movement.Tick(_target, canSee ? _attack.Range : 0f);

            // 선회하는 적도 사거리 안이면 움직이는 중에 쏴야 하므로 이동 뒤에 따로 판정한다
            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;

            if (canSee && toTarget.magnitude <= _attack.Range && _attack.IsReady)
            {
                _attack.Execute(_target);
            }
        }
    }
}
