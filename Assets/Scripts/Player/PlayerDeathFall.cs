using System.Collections;
using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    public class PlayerDeathFall : MonoBehaviour
    {
        [SerializeField] private Health _playerHealth;
        [SerializeField] private Transform _cameraTransform;
        // 마지막 시점 각도에서 얼마나 더 숙일지. 90에 가까우면 바닥을 정면으로 본다
        [SerializeField] private float _fallPitch = 80f;
        [SerializeField] private float _dropHeight = 1.1f;
        [SerializeField] private float _rollDegrees = 12f;
        // GameOverScreen의 표시 지연보다 짧아야 쓰러진 뒤에 화면이 뜬다
        [SerializeField] private float _duration = 0.9f;

        private void Awake()
        {
            if (_playerHealth == null || _cameraTransform == null)
            {
                Debug.LogError("PlayerDeathFall: Health와 카메라 참조가 비어 있습니다", this);
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

        private void OnDied()
        {
            StartCoroutine(Fall());
        }

        private IEnumerator Fall()
        {
            Vector3 startPosition = _cameraTransform.localPosition;
            float startPitch = NormalizeAngle(_cameraTransform.localEulerAngles.x);
            float endPitch = Mathf.Max(startPitch, _fallPitch);

            float elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;

                // 제곱으로 가속해야 버티다가 무너지듯 앞으로 고꾸라지는 느낌이 난다
                float t = Mathf.Clamp01(elapsed / _duration);
                float eased = t * t;

                _cameraTransform.localPosition = startPosition + Vector3.down * (_dropHeight * eased);
                _cameraTransform.localRotation = Quaternion.Euler(
                    Mathf.Lerp(startPitch, endPitch, eased), 0f, _rollDegrees * eased);

                yield return null;
            }
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
