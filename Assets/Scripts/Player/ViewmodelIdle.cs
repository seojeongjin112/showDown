using System;
using UnityEngine;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ViewmodelIdle : MonoBehaviour
    {
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _framesPerSecond = 8f;

        private SpriteRenderer _renderer;
        // 무기 스크립트들이 동작이 끝나면 씬에 놓인 정지 스프라이트로 되돌리므로 그것도 대기 상태로 본다
        private Sprite _restSprite;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();

            if (_frames == null || _frames.Length == 0)
            {
                Debug.LogError("ViewmodelIdle: 대기 프레임이 비어 있습니다", this);
                enabled = false;
                return;
            }

            _restSprite = _renderer.sprite;
        }

        // 발사·장전 같은 동작 프레임이 표시 중일 때는 건드리지 않고, 대기 자세일 때만 덮어쓴다.
        // 무기 스크립트의 Update 뒤에 실행해야 같은 프레임에 되돌려 놓은 정지 스프라이트를 바꿀 수 있다
        private void LateUpdate()
        {
            Sprite current = _renderer.sprite;
            if (current != _restSprite && Array.IndexOf(_frames, current) < 0)
            {
                return;
            }

            _renderer.sprite = _frames[(int)(Time.time * _framesPerSecond) % _frames.Length];
        }
    }
}
