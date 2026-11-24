using System.Collections.Generic;
using UnityEngine;

namespace ArenaBreak.Core
{
    // 횃불·램프·총알에 붙이는 점광. 유니티 조명을 쓰지 않고 ArenaLighting이 가까운 것만 골라 셰이더에 넘긴다.
    // 긴 벽 메시도 위치마다 따로 밝아지게 하려고 오브젝트 단위 조명 대신 이 방식을 쓴다
    [ExecuteAlways]
    public class ArenaLight : MonoBehaviour
    {
        private static readonly List<ArenaLight> Active = new List<ArenaLight>();

        [SerializeField] private Color _color = new Color(1f, 0.6f, 0.25f, 1f);
        [SerializeField] private float _intensity = 1f;
        [SerializeField] private float _range = 9f;
        [Tooltip("0이면 일정하고, 클수록 불꽃처럼 일렁인다")]
        [SerializeField, Range(0f, 1f)] private float _flicker = 0.15f;
        [SerializeField] private float _flickerSpeed = 7f;

        private float _seed;

        public static IReadOnlyList<ArenaLight> All => Active;

        public Color Color => _color;
        public float Range => _range;

        // 시간에 따라 깜빡이는 세기
        public float CurrentIntensity
        {
            get
            {
                if (_flicker <= 0f)
                {
                    return _intensity;
                }

                float noise = Mathf.PerlinNoise(_seed, Time.time * _flickerSpeed);
                return _intensity * (1f - _flicker + noise * _flicker * 2f);
            }
        }

        private void OnEnable()
        {
            _seed = Random.value * 100f;
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }
    }
}
