using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class TitleDustScroller : MonoBehaviour
    {
        [SerializeField] private RawImage _image;
        // UV 단위/초. x가 음수면 먼지가 오른쪽으로 날린다
        [SerializeField] private Vector2 _scrollSpeed = new Vector2(-0.3f, 0f);

        private void Update()
        {
            Rect uv = _image.uvRect;
            // 오래 켜 두어도 UV 값이 커지며 정밀도가 떨어지지 않도록 0~1로 되감는다
            uv.x = Mathf.Repeat(uv.x + _scrollSpeed.x * Time.deltaTime, 1f);
            uv.y = Mathf.Repeat(uv.y + _scrollSpeed.y * Time.deltaTime, 1f);
            _image.uvRect = uv;
        }
    }
}
