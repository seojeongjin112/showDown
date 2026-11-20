using UnityEngine;

namespace ArenaBreak.Enemy
{
    public class ChaseMovement : MonoBehaviour, IEnemyMovement
    {
        [SerializeField] private float _moveSpeed = 2f;

        public void Tick(Transform target, float attackRange)
        {
            // 높이 차이를 무시해야 플레이어 눈높이 쪽으로 적이 떠오르지 않는다
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= attackRange)
            {
                return;
            }

            transform.position += toTarget.normalized * (_moveSpeed * Time.deltaTime);
        }
    }
}
