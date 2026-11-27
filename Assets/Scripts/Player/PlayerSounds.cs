using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Player
{
    // 이동·체력 스크립트를 고치지 않고 이벤트만 들어서 플레이어 목소리를 낸다. 1인칭이라 모두 2D로 재생한다
    [RequireComponent(typeof(Health), typeof(FirstPersonMovement))]
    public class PlayerSounds : MonoBehaviour
    {
        [Tooltip("여러 개를 넣으면 맞을 때마다 하나를 골라 같은 신음이 반복되지 않게 한다")]
        [SerializeField] private AudioClip[] _hurt;
        [SerializeField] private AudioClip _jump;
        [SerializeField] private AudioClip _death;
        [SerializeField] private AudioClip _levelUp;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.8f;
        [Tooltip("연속으로 맞아도 신음이 겹쳐 나지 않게 하는 간격")]
        [SerializeField] private float _hurtCooldown = 0.35f;

        private Health _health;
        private FirstPersonMovement _movement;
        private Experience _experience;
        private int _lastHealth;
        private float _nextHurtTime;
        private int _lastLevelUpFrame = -1;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _movement = GetComponent<FirstPersonMovement>();
            // 튜토리얼 씬처럼 경험치가 없는 플레이어도 있을 수 있어 없으면 레벨업 소리만 건너뛴다
            _experience = GetComponent<Experience>();
        }

        private void OnEnable()
        {
            _health.Changed += OnHealthChanged;
            _health.Died += OnDied;
            _movement.Jumped += OnJumped;

            if (_experience != null)
            {
                _experience.LeveledUp += OnLeveledUp;
            }
        }

        private void OnDisable()
        {
            _health.Changed -= OnHealthChanged;
            _health.Died -= OnDied;
            _movement.Jumped -= OnJumped;

            if (_experience != null)
            {
                _experience.LeveledUp -= OnLeveledUp;
            }
        }

        private void Start()
        {
            _lastHealth = _health.Current;
        }

        // 체력 이벤트는 회복·최대 체력 강화에도 오므로 줄었을 때만 신음을 낸다
        private void OnHealthChanged(int current, int max)
        {
            bool damaged = current < _lastHealth;
            _lastHealth = current;

            // 죽는 일격에는 사망 소리만 나야 신음과 겹치지 않는다
            if (!damaged || _health.IsDead || Time.time < _nextHurtTime || _hurt == null || _hurt.Length == 0)
            {
                return;
            }

            _nextHurtTime = Time.time + _hurtCooldown;
            SfxPlayer.Play2D(_hurt[Random.Range(0, _hurt.Length)], _volume);
        }

        private void OnJumped()
        {
            SfxPlayer.Play2D(_jump, _volume);
        }

        private void OnDied()
        {
            SfxPlayer.Play2D(_death, _volume);
        }

        // 큰 경험치를 한 번에 얻으면 같은 프레임에 레벨이 여러 번 올라 소리가 겹겹이 쌓이므로 한 번만 낸다
        private void OnLeveledUp(int level)
        {
            if (Time.frameCount == _lastLevelUpFrame)
            {
                return;
            }

            _lastLevelUpFrame = Time.frameCount;
            SfxPlayer.Play2D(_levelUp, _volume);
        }
    }
}
