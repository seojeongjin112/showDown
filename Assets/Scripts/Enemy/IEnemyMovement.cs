using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 마차의 선회 사격처럼 "쫓다가 멈춤"이 아닌 이동을 갈아끼우기 위해 EnemyAI에서 이동을 분리한다
    public interface IEnemyMovement
    {
        void Tick(Transform target, float attackRange);
    }
}
