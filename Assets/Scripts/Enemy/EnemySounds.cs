using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    // 공격·체력 스크립트를 고치지 않고 이벤트만 들어서 적의 위치에서 소리를 낸다
    [RequireComponent(typeof(Health))]
    public class EnemySounds : MonoBehaviour
    {
        [SerializeField] private EnemySoundSet _sounds;
        [Tooltip("산탄 여러 알이 한 번에 맞아도 신음이 겹쳐 나지 않게 하는 간격")]
        [SerializeField] private float _hurtCooldown = 0.25f;
        [Tooltip("적이 한꺼번에 몰려와도 고함이 겹쳐 시끄럽지 않게, 모든 적이 함께 지키는 고함 간격")]
        [SerializeField] private float _voiceGap = 0.6f;

        // 적끼리 공유해야 떼로 나타날 때 함성이 한두 번만 들린다
        private static float _nextVoiceTime;

        private Health _health;
        private IEnemyAttack _attack;
        private RangedAttack _ranged;
        private int _lastHealth;
        private float _nextHurtTime;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _attack = GetComponent<IEnemyAttack>();
            _ranged = GetComponent<RangedAttack>();

            if (_sounds == null)
            {
                Debug.LogWarning("EnemySounds: 소리 묶음이 연결되지 않았습니다", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _health.Changed += OnHealthChanged;
            _health.Died += OnDied;

            // 원거리 적은 조준이 끝난 순간(Attacked)보다 실제로 쏘는 순간에 총소리가 나야 맞아 보인다
            if (_ranged != null)
            {
                _ranged.Fired += OnAttack;
            }
            else if (_attack != null)
            {
                _attack.Attacked += OnAttacked;
            }
        }

        private void OnDisable()
        {
            _health.Changed -= OnHealthChanged;
            _health.Died -= OnDied;

            if (_ranged != null)
            {
                _ranged.Fired -= OnAttack;
            }
            else if (_attack != null)
            {
                _attack.Attacked -= OnAttacked;
            }
        }

        private void Start()
        {
            _lastHealth = _health.Current;

            if (Random.value < _sounds.AlertChance)
            {
                PlayVoice(_sounds.RandomAlert);
            }
        }

        private void OnAttacked(Transform target)
        {
            OnAttack();
        }

        private void OnAttack()
        {
            SfxPlayer.PlayAt(_sounds.RandomAttack, transform.position, _sounds.Volume);

            if (Random.value < _sounds.AttackVoiceChance)
            {
                PlayVoice(_sounds.RandomAttackVoice);
            }
        }

        // 체력 이벤트는 회복에도 오므로 줄었을 때만 신음을 낸다
        private void OnHealthChanged(int current, int max)
        {
            bool damaged = current < _lastHealth;
            _lastHealth = current;

            if (!damaged || _health.IsDead || Time.time < _nextHurtTime)
            {
                return;
            }

            _nextHurtTime = Time.time + _hurtCooldown;
            SfxPlayer.PlayAt(_sounds.RandomHurt, transform.position, _sounds.Volume);
        }

        private void OnDied()
        {
            SfxPlayer.PlayAt(_sounds.RandomDeath, transform.position, _sounds.Volume);
        }

        private void PlayVoice(AudioClip clip)
        {
            if (clip == null || Time.time < _nextVoiceTime)
            {
                return;
            }

            _nextVoiceTime = Time.time + _voiceGap;
            SfxPlayer.PlayAt(clip, transform.position, _sounds.Volume);
        }
    }
}
