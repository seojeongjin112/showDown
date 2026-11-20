using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 마차의 선회 사격처럼 "쫓다가 멈춤"이 아닌 이동을 갈아끼우기 위해 EnemyAI에서 이동을 분리한다
    public interface IEnemyMovement
    {
        // attackRange가 0이면 시야가 가려져 쏠 수 없는 상태라는 뜻이므로, 어떻게든 길을 찾아 접근해야 한다
        void Tick(Transform target, float attackRange);
    }
}
