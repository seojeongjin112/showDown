using UnityEngine;

namespace ArenaBreak.Player
{
    // 고정 좌표로 두면 창 비율에 따라 팔이 화면 모서리에서 떨어져 보인다. 화면 크기에서 크기와 위치를 계산한다.
    // 에디터에서도 결과가 보이도록 ExecuteAlways로 둔다
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ViewmodelAnchor : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        // 카메라 근평면보다 멀고, 벽에 바짝 붙었을 때의 벽까지 거리(약 0.29)보다 가까워야 팔이 벽에 가려지지 않는다
        [SerializeField] private float _distance = 0.15f;
        // 팔 높이가 화면 높이에서 차지하는 비율
        [SerializeField] private float _heightFraction = 0.5f;
        // 소매 끝의 잘린 단면이 보이지 않도록 화면 밖으로 내보내는 비율(팔 높이 기준)
        [SerializeField] private float _overhangFraction = 0.08f;
        [Tooltip("켜면 왼쪽 아래 모서리에 붙인다. 양손 무기의 왼손 팔용")]
        [SerializeField] private bool _anchorLeft;

        private SpriteRenderer _renderer;

        // 걷기 흔들림처럼 기준 위치에 더하는 오프셋. 해상도와 무관하도록 팔 높이에 대한 비율로 받는다
        public Vector2 OffsetFraction { get; set; }

        // 피격 반동은 걷기 흔들림과 동시에 일어나므로 같은 값을 덮어쓰지 않도록 따로 둔다
        public Vector2 ImpactOffsetFraction { get; set; }

        private void OnEnable()
        {
            _renderer = GetComponent<SpriteRenderer>();

            if (_camera == null)
            {
                _camera = GetComponentInParent<Camera>();
            }
        }

        private void LateUpdate()
        {
            if (_camera == null || _renderer.sprite == null)
            {
                return;
            }

            float halfViewHeight = _distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfViewWidth = halfViewHeight * _camera.aspect;

            Vector2 spriteSize = _renderer.sprite.bounds.size;
            float armHeight = halfViewHeight * 2f * _heightFraction;
            float scale = armHeight / spriteSize.y;
            float armWidth = spriteSize.x * scale;
            float overhang = armHeight * _overhangFraction;

            Vector2 offset = (OffsetFraction + ImpactOffsetFraction) * armHeight;

            // 스프라이트 피벗이 중앙이므로 아래 모서리가 화면 모서리에 닿도록 반 크기만큼 안쪽으로 둔다
            float cornerX = halfViewWidth - armWidth * 0.5f + overhang;
            transform.localPosition = new Vector3(
                _anchorLeft ? -cornerX + offset.x : cornerX + offset.x,
                -halfViewHeight + armHeight * 0.5f - overhang + offset.y,
                _distance);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
