using UnityEngine;

namespace ArenaBreak.Core
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteLoopAnimation : MonoBehaviour
    {
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _framesPerSecond = 12f;

        private SpriteRenderer _renderer;
        private float _elapsed;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();

            if (_frames == null || _frames.Length == 0)
            {
                Debug.LogError("SpriteLoopAnimation: 프레임이 비어 있습니다", this);
                enabled = false;
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            int frameIndex = (int)(_elapsed * _framesPerSecond) % _frames.Length;
            _renderer.sprite = _frames[frameIndex];
        }
    }
}
