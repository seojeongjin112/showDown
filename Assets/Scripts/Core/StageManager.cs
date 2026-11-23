using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArenaBreak.Core
{
    public class StageManager : MonoBehaviour
    {
        [SerializeField] private StageData[] _stages;
        [SerializeField] private Light _sun;

        private int _currentIndex = -1;
        private Coroutine _timerRoutine;

        // HUD 표시용이라 1부터 센다
        public int CurrentStage => _currentIndex + 1;
        public int TotalStages => _stages != null ? _stages.Length : 0;
        public StageData CurrentData => _currentIndex >= 0 ? _stages[_currentIndex] : null;

        public event Action<int> StageStarted;
        public event Action<int> StageCleared;
        public event Action AllStagesCleared;

        // 매 프레임이 아니라 정수 초가 바뀔 때만 보내서 UI가 불필요하게 갱신되지 않게 한다
        public event Action<int> RemainingSecondsChanged;

        private void Start()
        {
            if (_stages == null || _stages.Length == 0)
            {
                Debug.LogError("StageManager: 스테이지 데이터가 비어 있다.", this);
                enabled = false;
                return;
            }

            StartStage(0);
        }

        // 도착 지역 트리거가 호출한다. 시간제 스테이지에서는 무시해 우연히 밟아도 클리어되지 않게 한다
        public void NotifyGoalReached()
        {
            if (CurrentData == null || CurrentData.ClearCondition != StageClearCondition.ReachZone)
            {
                return;
            }

            ClearCurrentStage();
        }

        private void StartStage(int index)
        {
            _currentIndex = index;
            StageData data = _stages[index];

            ApplyLighting(data);
            StageStarted?.Invoke(CurrentStage);

            if (data.ClearCondition == StageClearCondition.SurviveTime)
            {
                _timerRoutine = StartCoroutine(RunSurviveTimer(data.SurviveSeconds));
            }
        }

        private void ClearCurrentStage()
        {
            if (_timerRoutine != null)
            {
                StopCoroutine(_timerRoutine);
                _timerRoutine = null;
            }

            int cleared = CurrentStage;
            StageCleared?.Invoke(cleared);

            if (_currentIndex + 1 >= _stages.Length)
            {
                AllStagesCleared?.Invoke();
                return;
            }

            StartStage(_currentIndex + 1);
        }

        private IEnumerator RunSurviveTimer(float seconds)
        {
            float remaining = seconds;
            int lastReported = -1;

            while (remaining > 0f)
            {
                int whole = Mathf.CeilToInt(remaining);
                if (whole != lastReported)
                {
                    lastReported = whole;
                    RemainingSecondsChanged?.Invoke(whole);
                }

                remaining -= Time.deltaTime;
                yield return null;
            }

            _timerRoutine = null;
            ClearCurrentStage();
        }

        private void ApplyLighting(StageData data)
        {
            if (data.Skybox != null)
            {
                RenderSettings.skybox = data.Skybox;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = data.AmbientColor;

            if (_sun == null)
            {
                return;
            }

            _sun.color = data.SunColor;
            _sun.intensity = data.SunIntensity;
            _sun.transform.rotation = Quaternion.Euler(data.SunEulerAngles);
        }
    }
}
