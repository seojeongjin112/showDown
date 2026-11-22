using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    public static class ProjectileObstacle
    {
        private const int BufferSize = 8;

        // 매 프레임 호출되므로 결과 배열을 재사용해 할당을 피한다
        private static readonly RaycastHit[] Hits = new RaycastHit[BufferSize];

        // 플레이어와 적은 투사체가 거리로 따로 판정하거나 서로 막지 않아야 해서, 피해를 받는 대상이 아닌 콜라이더만 장애물로 본다
        public static bool IsBlocked(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            if (distance < Mathf.Epsilon)
            {
                return false;
            }

            int count = Physics.RaycastNonAlloc(from, delta / distance, Hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                if (Hits[i].collider.GetComponentInParent<IDamageable>() == null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
