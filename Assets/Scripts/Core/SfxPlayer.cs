using UnityEngine;
using UnityEngine.Audio;

namespace ArenaBreak.Core
{
    // 총소리처럼 짧은 소리가 한꺼번에 많이 나므로, 매번 AudioSource를 만들지 않고 미리 만든 소스를 돌려 쓴다.
    // 음악 관리자와 같은 프리팹에 붙어 SceneMusic이 함께 만들고, 씬이 바뀌어도 살아남는다
    public class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup _output;
        [SerializeField] private int _poolSize = 24;
        [Tooltip("같은 소리가 반복돼도 기계적으로 들리지 않게 음높이를 이만큼 흔든다")]
        [SerializeField, Range(0f, 0.3f)] private float _pitchJitter = 0.06f;

        [Header("3D")]
        [SerializeField] private float _minDistance = 2f;
        [SerializeField] private float _maxDistance = 40f;

        private static SfxPlayer _instance;
        private static bool _warnedMissing;

        private AudioSource[] _pool;
        private int _next;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            _pool = new AudioSource[Mathf.Max(1, _poolSize)];

            for (int i = 0; i < _pool.Length; i++)
            {
                // 3D 소리는 소스 위치에서 나야 해서 소스마다 따로 옮길 수 있는 자식 오브젝트를 둔다
                GameObject child = new GameObject("Sfx" + i);
                child.transform.SetParent(transform, false);

                AudioSource source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.outputAudioMixerGroup = _output;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = _minDistance;
                source.maxDistance = _maxDistance;
                _pool[i] = source;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        // 1인칭 무기·UI처럼 듣는 사람 귀에서 나는 소리
        public static void Play2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null || !HasInstance())
            {
                return;
            }

            _instance.PlayInternal(clip, Vector3.zero, volume, 0f);
        }

        // 적의 총소리·폭발처럼 멀리 있으면 작게 들려야 하는 소리
        public static void PlayAt(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null || !HasInstance())
            {
                return;
            }

            _instance.PlayInternal(clip, position, volume, 1f);
        }

        private static bool HasInstance()
        {
            if (_instance != null)
            {
                return true;
            }

            // 소리마다 경고하면 콘솔이 넘치므로 한 번만 알린다
            if (!_warnedMissing)
            {
                _warnedMissing = true;
                Debug.LogWarning("SfxPlayer: 효과음 재생기가 없습니다. 씬에 SceneMusic이 있는지 확인하세요");
            }

            return false;
        }

        private void PlayInternal(AudioClip clip, Vector3 position, float volume, float spatialBlend)
        {
            AudioSource source = TakeSource();
            source.transform.position = position;
            source.spatialBlend = spatialBlend;
            source.pitch = 1f + Random.Range(-_pitchJitter, _pitchJitter);
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.Play();
        }

        // 쉬는 소스를 먼저 쓰고, 모두 울리는 중이면 가장 오래전에 쓴 소스를 끊고 쓴다
        private AudioSource TakeSource()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                int index = (_next + i) % _pool.Length;
                if (!_pool[index].isPlaying)
                {
                    _next = (index + 1) % _pool.Length;
                    return _pool[index];
                }
            }

            AudioSource oldest = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            return oldest;
        }
    }
}
