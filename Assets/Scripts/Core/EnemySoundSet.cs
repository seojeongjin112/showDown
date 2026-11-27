using UnityEngine;

namespace ArenaBreak.Core
{
    // 같은 종족(인디언·무법자·늑대 등)은 소리를 함께 쓰므로 프리팹마다 넣지 않고 묶음 하나를 공유한다
    [CreateAssetMenu(fileName = "EnemySounds", menuName = "ArenaBreak/Enemy Sound Set")]
    public class EnemySoundSet : ScriptableObject
    {
        [Tooltip("총소리·무는 소리·휘두르는 소리처럼 공격할 때마다 나는 소리")]
        [SerializeField] private AudioClip[] _attack;
        [Tooltip("공격하면서 지르는 고함. 매번 지르면 시끄러워서 확률로 낸다")]
        [SerializeField] private AudioClip[] _attackVoice;
        [SerializeField, Range(0f, 1f)] private float _attackVoiceChance = 0.3f;
        [Tooltip("나타날 때 지르는 함성·울음")]
        [SerializeField] private AudioClip[] _alert;
        [SerializeField, Range(0f, 1f)] private float _alertChance = 0.5f;
        [SerializeField] private AudioClip[] _hurt;
        [SerializeField] private AudioClip[] _death;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.9f;

        public float AttackVoiceChance => _attackVoiceChance;
        public float AlertChance => _alertChance;
        public float Volume => _volume;

        public AudioClip RandomAttack => Pick(_attack);
        public AudioClip RandomAttackVoice => Pick(_attackVoice);
        public AudioClip RandomAlert => Pick(_alert);
        public AudioClip RandomHurt => Pick(_hurt);
        public AudioClip RandomDeath => Pick(_death);

        // 같은 소리만 반복되면 금방 귀에 거슬려서 여러 개 중 하나를 고른다
        private static AudioClip Pick(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            return clips[Random.Range(0, clips.Length)];
        }
    }
}
