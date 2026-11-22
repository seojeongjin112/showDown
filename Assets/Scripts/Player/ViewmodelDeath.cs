using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ViewmodelDeath : MonoBehaviour
    {
        [SerializeField] private Health _playerHealth;
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _framesPerSecond = 12f;

        private SpriteRenderer _renderer;
        private float _elapsed;
        private bool _playing;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();

            if (_playerHealth == null)
            {
                _playerHealth = GetComponentInParent<Health>();
            }

            if (_playerHealth == null || _frames == null || _frames.Length == 0)
            {
                Debug.LogError("ViewmodelDeath: Health 참조 또는 사망 프레임이 비어 있습니다", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _playerHealth.Died += OnDied;
        }

        private void OnDisable()
        {
            _playerHealth.Died -= OnDied;
        }

        private void Update()
        {
            if (!_playing)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            int index = Mathf.Min((int)(_elapsed * _framesPerSecond), _frames.Length - 1);
            _renderer.sprite = _frames[index];
        }

        private void OnDied()
        {
            // 발사·장전 등 다른 스크립트가 같은 SpriteRenderer를 매 프레임 덮어쓰므로 사망 연출 동안은 모두 끈다
            foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour != this && !(behaviour is ViewmodelAnchor))
                {
                    behaviour.enabled = false;
                }
            }

            _playing = true;
        }
    }
}
