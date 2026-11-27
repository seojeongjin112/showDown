using UnityEngine;

namespace ArenaBreak.Core
{
    public enum DifficultyTarget
    {
        Player,
        Enemy
    }

    // 스포너마다 생성 경로가 달라서(웨이브·조우·주기 스폰·보스 단계) 생성하는 쪽이 아니라 프리팹 스스로 난이도를 적용한다.
    // 보스 루트와 단계 프리팹이 같은 배율을 받아야 두 체력이 어긋나지 않는다
    [RequireComponent(typeof(Health))]
    public class DifficultyScaler : MonoBehaviour
    {
        [SerializeField] private DifficultyTarget _target = DifficultyTarget.Enemy;

        // 스포너와 보스가 Instantiate 직후 Max를 읽으므로 Start가 아니라 Awake에서 적용한다
        private void Awake()
        {
            Health health = GetComponent<Health>();

            if (_target == DifficultyTarget.Player)
            {
                health.ScaleMax(DifficultySelection.PlayerHealthMultiplier);
                health.SetIncomingDamageMultiplier(DifficultySelection.IncomingDamageMultiplier);
                return;
            }

            health.ScaleMax(DifficultySelection.EnemyHealthMultiplier);
        }
    }
}
