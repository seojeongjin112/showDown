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
        // 카메라 근평면보다 멀고 벽보다 가까워야 한다
        [SerializeField] private float _distance = 0.6f;
        // 팔 높이가 화면 높이에서 차지하는 비율
        [SerializeField] private float _heightFraction = 0.5f;
        // 소매 끝의 잘린 단면이 보이지 않도록 화면 밖으로 내보내는 비율(팔 높이 기준)
        [SerializeField] private float _overhangFraction = 0.08f;

        private SpriteRenderer _renderer;

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

            // 스프라이트 피벗이 중앙이므로 오른쪽 아래 모서리가 화면 모서리에 닿도록 반 크기만큼 안쪽으로 둔다
            transform.localPosition = new Vector3(
                halfViewWidth - armWidth * 0.5f + overhang,
                -halfViewHeight + armHeight * 0.5f - overhang,
                _distance);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
