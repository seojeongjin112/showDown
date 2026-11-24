using System;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class WaveHUD : MonoBehaviour
    {
        // 이벤트가 어느 구역에서 왔는지 알려 주지 않아서 구역마다 따로 핸들러를 걸어 둔다
        private class ArenaBinding
        {
            public WaveArena Arena;
            public Action<int> OnStarted;
            public Action<int> OnCleared;
        }

        [SerializeField] private WaveSpawner _spawner;
        [Tooltip("구역형 웨이브(WaveArena). 스테이지마다 하나씩 넣으면 진행 중인 구역을 따라간다")]
        [SerializeField] private WaveArena[] _arenas;
        [SerializeField] private Text _label;
        [Tooltip("왼쪽부터 웨이브 1칸씩. 꺼진 별은 프레임에 그려진 빈 슬롯이 보인다")]
        [SerializeField] private Image[] _stars;
        [SerializeField] private Sprite _doneSprite;
        [SerializeField] private Sprite _currentSprite;
        [Tooltip("{0}은 현재 웨이브, {1}은 전체 웨이브")]
        [SerializeField] private string _labelFormat = "웨이브 {0}/{1}";

        private ArenaBinding[] _bindings;
        private int _currentWave;
        private int _clearedWave;
        private int _totalWaves;
        private WaveArena _activeArena;

        private void OnEnable()
        {
            if (_spawner != null)
            {
                _spawner.WaveStarted += OnWaveStarted;
                _spawner.WaveCleared += OnWaveCleared;

                // HUD가 스포너보다 늦게 켜질 수 있어서 이미 진행된 웨이브부터 이어서 그린다
                _currentWave = _spawner.CurrentWave;
                _totalWaves = _spawner.TotalWaves;
            }

            BindArenas();
            Refresh();
        }

        private void OnDisable()
        {
            if (_spawner != null)
            {
                _spawner.WaveStarted -= OnWaveStarted;
                _spawner.WaveCleared -= OnWaveCleared;
            }

            UnbindArenas();
        }

        private void BindArenas()
        {
            if (_arenas == null)
            {
                return;
            }

            _bindings = new ArenaBinding[_arenas.Length];

            for (int i = 0; i < _arenas.Length; i++)
            {
                WaveArena arena = _arenas[i];
                if (arena == null)
                {
                    continue;
                }

                var binding = new ArenaBinding { Arena = arena };
                binding.OnStarted = wave => OnArenaWaveStarted(arena, wave);
                binding.OnCleared = wave => OnArenaWaveCleared(arena, wave);

                arena.WaveStarted += binding.OnStarted;
                arena.WaveCleared += binding.OnCleared;
                _bindings[i] = binding;
            }
        }

        private void UnbindArenas()
        {
            if (_bindings == null)
            {
                return;
            }

            for (int i = 0; i < _bindings.Length; i++)
            {
                ArenaBinding binding = _bindings[i];
                if (binding == null || binding.Arena == null)
                {
                    continue;
                }

                binding.Arena.WaveStarted -= binding.OnStarted;
                binding.Arena.WaveCleared -= binding.OnCleared;
            }

            _bindings = null;
        }

        private void OnArenaWaveStarted(WaveArena arena, int wave)
        {
            // 다른 구역으로 넘어오면 앞 구역의 클리어 표시가 남지 않게 초기화한다
            if (_activeArena != arena)
            {
                _activeArena = arena;
                _clearedWave = 0;
            }

            _totalWaves = arena.TotalWaves;
            OnWaveStarted(wave);
        }

        private void OnArenaWaveCleared(WaveArena arena, int wave)
        {
            if (_activeArena == arena)
            {
                OnWaveCleared(wave);
            }
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
            _label.text = string.Format(_labelFormat, _currentWave, _totalWaves);

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
