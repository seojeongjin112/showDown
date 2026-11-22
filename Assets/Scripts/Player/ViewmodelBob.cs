using UnityEngine;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(ViewmodelAnchor))]
    public class ViewmodelBob : MonoBehaviour
    {
        [SerializeField] private CharacterController _controller;
        // 걸음 수는 시간이 아니라 이동 거리에 비례해야 속도 버프를 받아도 발걸음과 흔들림이 맞는다
        [SerializeField] private float _stepsPerMeter = 0.5f;
        // 이 속도 이상이면 흔들림이 최대 진폭이 된다
        [SerializeField] private float _fullBobSpeed = 5f;
        // 팔 높이에 대한 비율
        [SerializeField] private float _horizontalAmplitude = 0.02f;
        [SerializeField] private float _verticalAmplitude = 0.03f;
        // 멈추거나 점프할 때 흔들림이 뚝 끊기지 않고 가라앉는 속도
        [SerializeField] private float _blendSpeed = 8f;

        private ViewmodelAnchor _anchor;
        private float _phase;
        private float _weight;

        private void Awake()
        {
            _anchor = GetComponent<ViewmodelAnchor>();

            if (_controller == null)
            {
                _controller = GetComponentInParent<CharacterController>();
            }

            if (_controller == null)
            {
                Debug.LogError("ViewmodelBob: 부모에서 CharacterController를 찾지 못했습니다", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            _anchor.OffsetFraction = Vector2.zero;
        }

        private void Update()
        {
            Vector3 velocity = _controller.velocity;
            velocity.y = 0f;
            float speed = _controller.isGrounded ? velocity.magnitude : 0f;

            float targetWeight = Mathf.Clamp01(speed / _fullBobSpeed);
            _weight = Mathf.Lerp(_weight, targetWeight, _blendSpeed * Time.deltaTime);
            _phase += speed * _stepsPerMeter * Time.deltaTime * 2f * Mathf.PI;

            // 가로는 한 걸음당 한 번, 세로는 두 번 흔들려야 8자 궤적의 걷는 느낌이 난다
            float x = Mathf.Sin(_phase) * _horizontalAmplitude * _weight;
            float y = Mathf.Sin(_phase * 2f) * _verticalAmplitude * _weight;
            _anchor.OffsetFraction = new Vector2(x, y);
        }
    }
}
