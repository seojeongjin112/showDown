using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArenaBreak.Core
{
    [CreateAssetMenu(fileName = "Wave", menuName = "ArenaBreak/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [Serializable]
        public class SpawnEntry
        {
            [SerializeField] private GameObject _enemyPrefab;
            [SerializeField] private int _count = 1;

            public GameObject EnemyPrefab => _enemyPrefab;
            public int Count => _count;
        }

        [SerializeField] private List<SpawnEntry> _entries = new List<SpawnEntry>();

        // 한꺼번에 스폰하면 적이 한 점에 겹쳐 나와서 간격을 두고 내보낸다
        [SerializeField] private float _spawnInterval = 0.5f;

        public IReadOnlyList<SpawnEntry> Entries => _entries;
        public float SpawnInterval => _spawnInterval;

        public int TotalCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _entries.Count; i++)
                {
                    total += _entries[i].Count;
                }
                return total;
            }
        }

        private void OnValidate()
        {
            _spawnInterval = Mathf.Max(0f, _spawnInterval);
        }
    }
}
