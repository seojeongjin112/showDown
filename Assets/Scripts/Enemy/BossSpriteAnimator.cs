using ArenaBreak.Core;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    public enum BossClip
    {
        Walk,
        Attack,
        WalkAttack,
        Slam,
        Charge,
        Summon,
        Roar,
        Enrage,
        Death,
        // 값을 바꾸면 이미 저장된 프리팹의 모션 번호가 어긋나서 새 모션은 항상 끝에 붙인다
        Barrage,
        DynamiteThrow
    }

    // 보스는 모션이 10종이라 걷기·사격·장전만 아는 EnemySpriteAnimator로는 부족해서 이름 붙은 모션을 AI가 직접 재생하게 한다
    public class BossSpriteAnimator : MonoBehaviour
    {
        [System.Serializable]
        private class Clip
        {
            [SerializeField] private BossClip _id;
            [SerializeField] private Sprite[] _frames;

            public BossClip Id => _id;
            public Sprite[] Frames => _frames;
        }

        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Clip[] _clips;
        [SerializeField] private float _framesPerSecond = 12f;

        private Health _health;
        private Sprite[][] _byId;
        private BossClip _current = BossClip.Walk;
        private float _elapsed;
        private bool _looping = true;
        private bool _dead;

        private void Awake()
        {
            _health = GetComponent<Health>();

            // 열거형 값으로 바로 찾도록 한 번만 배열로 펼쳐 둔다
            _byId = new Sprite[System.Enum.GetValues(typeof(BossClip)).Length][];
            if (_clips != null)
            {
                for (int i = 0; i < _clips.Length; i++)
                {
                    _byId[(int)_clips[i].Id] = _clips[i].Frames;
                }
            }

            if (_renderer == null || _health == null || IsEmpty(_byId[(int)BossClip.Walk]))
            {
                Debug.LogError("BossSpriteAnimator: 렌더러, Health 또는 걷기 모션이 비어 있습니다", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
        }

        public float Duration(BossClip clip)
        {
            Sprite[] frames = _byId[(int)clip];
            return IsEmpty(frames) ? 0f : frames.Length / _framesPerSecond;
        }

        // 걷기는 반복하고 나머지는 한 번 재생한 뒤 마지막 프레임에서 멈춘다. 끝나면 AI가 걷기로 되돌린다
        public void Play(BossClip clip)
        {
            if (_dead || IsEmpty(_byId[(int)clip]))
            {
                return;
            }

            _current = clip;
            _elapsed = 0f;
            _looping = clip == BossClip.Walk;
        }

        private void Update()
        {
            Sprite[] frames = _byId[(int)_current];
            _elapsed += Time.deltaTime;

            int index = (int)(_elapsed * _framesPerSecond);
            index = _looping ? index % frames.Length : Mathf.Min(index, frames.Length - 1);
            _renderer.sprite = frames[index];
        }

        private void OnDied()
        {
            Play(BossClip.Death);
            _dead = true;
        }

        private static bool IsEmpty(Sprite[] frames)
        {
            return frames == null || frames.Length == 0;
        }
    }
}
