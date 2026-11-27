using UnityEngine;

namespace ArenaBreak.Core
{
    // 한 판의 기록(플레이 시간, 처치 수). 승리·패배 화면이 끝날 때 읽어 간다
    public class RunStats : MonoBehaviour
    {
        private int _kills;
        private float _endTime = -1f;

        public int Kills => _kills;

        // 레벨업 카드를 고르는 동안처럼 시간이 멈춘 구간은 빼고 센다
        public float ElapsedSeconds => _endTime >= 0f ? _endTime : Time.timeSinceLevelLoad;

        private void OnEnable()
        {
            Health.AnyDied += OnAnyDied;
        }

        private void OnDisable()
        {
            Health.AnyDied -= OnAnyDied;
        }

        // 결과 화면이 뜨는 순간 기록을 멈춰서, 화면을 보는 동안 시간이 더 쌓이지 않게 한다
        public void Stop()
        {
            if (_endTime < 0f)
            {
                _endTime = Time.timeSinceLevelLoad;
            }
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        private void OnAnyDied(Health health)
        {
            if (_endTime >= 0f || health.CompareTag("Player"))
            {
                return;
            }

            // 보스는 루트(BossEncounter)와 단계가 함께 쓰러져서, 루트만 한 마리로 센다
            if (health.IsBoss && !health.TryGetComponent(out BossEncounter _))
            {
                return;
            }

            _kills++;
        }
    }
}
