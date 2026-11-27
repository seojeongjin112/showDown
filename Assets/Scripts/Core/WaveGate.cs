using System;
using UnityEngine;

namespace ArenaBreak.Core
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class WaveGate : MonoBehaviour
    {
        [SerializeField] private Sprite _closedSprite;
        [SerializeField] private Sprite _openSprite;
        [SerializeField] private Sprite[] _openingFrames;
        [SerializeField] private Sprite[] _closingFrames;
        [SerializeField] private float _framesPerSecond = 10f;
        // 그림은 문틀이 열려 있어도 보이므로 통행은 별도 콜라이더 하나로만 막는다
        [SerializeField] private Collider _blocker;
        [SerializeField] private bool _startsOpen = true;

        private SpriteRenderer _renderer;
        private Sprite[] _playing;
        private float _elapsed;
        private bool _targetOpen;

        public bool IsOpen => _targetOpen;
        public bool IsAnimating => _playing != null;

        // 애니메이션이 끝난 뒤에 알려야 웨이브 시작이 문이 닫히는 연출과 겹치지 않는다
        public event Action<bool> Finished;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();

            if (_closedSprite == null || _openSprite == null || _blocker == null)
            {
                Debug.LogError("WaveGate: 스프라이트 또는 통행 차단 콜라이더가 비어 있다.", this);
                enabled = false;
                return;
            }

            ApplyState(_startsOpen);
        }

        public void Open()
        {
            StartTransition(true, _openingFrames);
        }

        public void Close()
        {
            StartTransition(false, _closingFrames);
        }

        private void Update()
        {
            if (_playing == null)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            int index = (int)(_elapsed * _framesPerSecond);

            if (index >= _playing.Length)
            {
                Complete();
                return;
            }

            _renderer.sprite = _playing[index];
        }

        private void StartTransition(bool open, Sprite[] frames)
        {
            if (!enabled || _targetOpen == open && _playing == null)
            {
                return;
            }

            _targetOpen = open;

            // 열리는 순간부터 통과할 수 있어야 하고, 닫을 때는 연출 중에도 이미 막혀 있어야 끼이지 않는다
            _blocker.enabled = !open;

            if (frames == null || frames.Length == 0)
            {
                Complete();
                return;
            }

            _playing = frames;
            _elapsed = 0f;
        }

        private void Complete()
        {
            _playing = null;
            ApplyState(_targetOpen);
            Finished?.Invoke(_targetOpen);
        }

        private void ApplyState(bool open)
        {
            _targetOpen = open;
            _blocker.enabled = !open;
            _renderer.sprite = open ? _openSprite : _closedSprite;
        }
    }
}
