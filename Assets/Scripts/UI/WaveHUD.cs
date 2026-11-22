using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class WaveHUD : MonoBehaviour
    {
        [SerializeField] private WaveSpawner _spawner;
        [SerializeField] private Text _label;
        [Tooltip("왼쪽부터 웨이브 1칸씩. 꺼진 별은 프레임에 그려진 빈 슬롯이 보인다")]
        [SerializeField] private Image[] _stars;
        [SerializeField] private Sprite _doneSprite;
        [SerializeField] private Sprite _currentSprite;
        [Tooltip("{0}은 현재 웨이브, {1}은 전체 웨이브")]
        [SerializeField] private string _labelFormat = "웨이브 {0}/{1}";

        private int _currentWave;
        private int _clearedWave;

        private void OnEnable()
        {
            if (_spawner == null)
            {
                return;
            }

            _spawner.WaveStarted += OnWaveStarted;
            _spawner.WaveCleared += OnWaveCleared;

            // HUD가 스포너보다 늦게 켜질 수 있어서 이미 진행된 웨이브부터 이어서 그린다
            _currentWave = _spawner.CurrentWave;
            Refresh();
        }

        private void OnDisable()
        {
            if (_spawner == null)
            {
                return;
            }

            _spawner.WaveStarted -= OnWaveStarted;
            _spawner.WaveCleared -= OnWaveCleared;
        }

        private void OnWaveStarted(int wave)
        {
            _currentWave = wave;
            Refresh();
        }

        private void OnWaveCleared(int wave)
        {
            _clearedWave = wave;
            Refresh();
        }

        private void Refresh()
        {
            _label.text = string.Format(_labelFormat, _currentWave, _spawner.TotalWaves);

            for (int i = 0; i < _stars.Length; i++)
            {
                int wave = i + 1;

                if (wave <= _clearedWave)
                {
                    _stars[i].enabled = true;
                    _stars[i].sprite = _doneSprite;
                }
                else if (wave == _currentWave)
                {
                    _stars[i].enabled = true;
                    _stars[i].sprite = _currentSprite;
                }
                else
                {
                    _stars[i].enabled = false;
                }
            }
        }
    }
}
