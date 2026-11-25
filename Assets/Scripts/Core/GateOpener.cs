using UnityEngine;

namespace ArenaBreak.Core
{
    // 웨이브 구역을 모두 깨면 다른 문(보스 문)을 연다. 웨이브 구역 자신의 출구와는 별개의 문이다
    public class GateOpener : MonoBehaviour
    {
        [SerializeField] private WaveArena _arena;
        [SerializeField] private WaveGate _gate;

        private void OnEnable()
        {
            if (_arena != null)
            {
                _arena.AllWavesCleared += OnAllWavesCleared;
            }
        }

        private void OnDisable()
        {
            if (_arena != null)
            {
                _arena.AllWavesCleared -= OnAllWavesCleared;
            }
        }

        private void OnAllWavesCleared()
        {
            if (_gate != null)
            {
                _gate.Open();
            }
        }
    }
}
