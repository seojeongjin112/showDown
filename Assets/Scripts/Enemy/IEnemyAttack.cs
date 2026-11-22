using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 이동과 공격을 분리해 두면 말·마차 적이 "빠른 이동 + 기존 공격"으로 조합된다
    public interface IEnemyAttack
    {
        float Range { get; }
        bool IsReady { get; }
        event System.Action<Transform> Attacked;
        void Execute(Transform target);
    }
}
