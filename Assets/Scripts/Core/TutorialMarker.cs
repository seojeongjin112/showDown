using UnityEngine;

namespace ArenaBreak.Core
{
    // 지금 상대할 표적 머리 위에 번호가 붙은 화살표를 띄워서, 허수아비가 여럿이어도 어느 것부터 해야 하는지 알려 준다
    public class TutorialMarker : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _arrow;
        [Tooltip("화살표 위에 번호를 띄울 글자(선택). 비워 두면 번호는 안내판 제목에만 나온다")]
        [SerializeField] private TextMesh _number;
        [Tooltip("표적 발밑에서 화살표 끝까지의 높이")]
        [SerializeField] private float _height = 3.4f;
        [SerializeField] private float _bobAmplitude = 0.25f;
        [SerializeField] private float _bobSpeed = 4f;

        private Transform _target;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public void Show(Transform target, int number)
        {
            _target = target;
            if (_number != null)
            {
                _number.text = number > 0 ? number.ToString() : string.Empty;
            }

            gameObject.SetActive(true);
            Follow();
        }

        public void Hide()
        {
            _target = null;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            // 표적이 사라지면(쓰러지거나 치워지면) 화살표도 함께 끈다
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            Follow();
        }

        private void Follow()
        {
            float bob = Mathf.Sin(Time.unscaledTime * _bobSpeed) * _bobAmplitude;
            transform.position = _target.position + Vector3.up * (_height + bob);
        }
    }
}
