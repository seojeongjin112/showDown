using UnityEngine;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(GunWeapon))]
    public class MuzzleFlashAnimation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _flash;
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _framesPerSecond = 30f;

        private GunWeapon _gun;
        private float _elapsed;
        private bool _isPlaying;

        private void Awake()
        {
            _gun = GetComponent<GunWeapon>();

            if (_flash == null || _frames == null || _frames.Length == 0)
            {
                Debug.LogError("MuzzleFlashAnimation: 화염 렌더러 또는 프레임이 비어 있습니다", this);
                enabled = false;
                return;
            }

            _flash.enabled = false;
        }

        private void OnEnable()
        {
            _gun.Fired += OnFired;
        }

        private void OnDisable()
        {
            _gun.Fired -= OnFired;

            // 재생 도중에 무기를 바꾸면 화염이 다음에 꺼낼 때까지 남아 있지 않도록 끈다
            _isPlaying = false;
            _flash.enabled = false;
        }

        private void Update()
        {
            if (!_isPlaying)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            int index = (int)(_elapsed * _framesPerSecond);

            if (index >= _frames.Length)
            {
                _isPlaying = false;
                _flash.enabled = false;
                return;
            }

            _flash.sprite = _frames[index];
        }

        private void OnFired()
        {
            _elapsed = 0f;
            _isPlaying = true;
            _flash.sprite = _frames[0];
            _flash.enabled = true;
        }
    }
}
