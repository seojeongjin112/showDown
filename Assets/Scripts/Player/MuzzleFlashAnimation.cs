using System;
using UnityEngine;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(GunWeapon))]
    public class MuzzleFlashAnimation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _flash;
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _framesPerSecond = 30f;
        // 발사 모션이 총을 흔들어서 총구 끝이 프레임마다 움직이므로, 팔 프레임마다 총구 위치(스프라이트 로컬 단위)를 따로 둔다
        [SerializeField] private Sprite[] _armFrames;
        [SerializeField] private Vector2[] _muzzlePositions;

        private GunWeapon _gun;
        private SpriteRenderer _arm;
        private float _elapsed;
        private bool _isPlaying;

        private void Awake()
        {
            _gun = GetComponent<GunWeapon>();
            _arm = GetComponent<SpriteRenderer>();

            if (_flash == null || _frames == null || _frames.Length == 0
                || _armFrames == null || _muzzlePositions == null || _armFrames.Length != _muzzlePositions.Length)
            {
                Debug.LogError("MuzzleFlashAnimation: 화염 렌더러, 프레임, 총구 위치 설정이 올바르지 않습니다", this);
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

        // 팔 스프라이트는 무기 스크립트의 Update에서 바뀌므로 그 뒤에 위치를 맞춰야 같은 프레임에 어긋나지 않는다
        private void LateUpdate()
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
            FollowMuzzle();
        }

        private void FollowMuzzle()
        {
            int armIndex = Array.IndexOf(_armFrames, _arm.sprite);
            if (armIndex >= 0)
            {
                Vector2 muzzle = _muzzlePositions[armIndex];
                Vector3 position = _flash.transform.localPosition;
                position.x = muzzle.x;
                position.y = muzzle.y;
                _flash.transform.localPosition = position;
            }
        }

        private void OnFired()
        {
            _elapsed = 0f;
            _isPlaying = true;
            _flash.sprite = _frames[0];
            _flash.enabled = true;
            FollowMuzzle();
        }
    }
}
