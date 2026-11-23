using UnityEngine;

namespace ArenaBreak.Core
{
    [RequireComponent(typeof(Collider))]
    public class StageGoalZone : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [SerializeField] private StageManager _stageManager;

        // 스테이지 1·2가 같은 맵에 이어져 있어 도착 지역도 둘 다 씬에 놓이므로,
        // 자기 차례가 아닌 지역은 밟아도 반응하지 않게 담당 스테이지를 지정한다
        [Tooltip("이 지역이 클리어 조건인 스테이지 번호 (1부터)")]
        [SerializeField] private int _targetStage = 1;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(PlayerTag))
            {
                return;
            }

            if (_stageManager == null)
            {
                Debug.LogError("StageGoalZone: StageManager가 연결되지 않았다.", this);
                return;
            }

            if (_stageManager.CurrentStage != _targetStage)
            {
                return;
            }

            _stageManager.NotifyGoalReached();
        }
    }
}
