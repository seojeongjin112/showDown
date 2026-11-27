using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace ArenaBreak.Core
{
    // 타이틀에서 게임으로 넘어갈 때 음악이 뚝 끊기지 않도록 씬이 바뀌어도 살아남는 하나의 재생기로 둔다
    public class MusicManager : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup _output;
        [SerializeField] private float _defaultFadeSeconds = 1.5f;

        private static MusicManager _instance;

        // 곡을 바꿀 때 이전 곡을 줄이면서 새 곡을 키우려면 소스가 두 개 필요하다
        private AudioSource[] _sources;
        private int _activeIndex;
        private Coroutine _fadeRoutine;

        public static MusicManager Instance => _instance;
        public AudioClip CurrentClip => _sources[_activeIndex].clip;

        private void Awake()
        {
            // 각 씬이 관리자가 없을 때를 대비해 프리팹을 들고 있으므로, 이미 있으면 새로 생긴 쪽을 지운다
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _sources = new AudioSource[2];
            for (int i = 0; i < _sources.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
                source.volume = 0f;
                source.outputAudioMixerGroup = _output;
                _sources[i] = source;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void Play(AudioClip clip)
        {
            Play(clip, _defaultFadeSeconds);
        }

        public void Play(AudioClip clip, float fadeSeconds)
        {
            if (clip == null)
            {
                Stop(fadeSeconds);
                return;
            }

            AudioSource current = _sources[_activeIndex];

            // 같은 곡을 쓰는 다음 씬이나 같은 스테이지 이벤트가 다시 와도 처음부터 다시 틀지 않는다
            if (current.clip == clip && current.isPlaying)
            {
                return;
            }

            _activeIndex = 1 - _activeIndex;
            AudioSource next = _sources[_activeIndex];
            next.clip = clip;
            next.volume = 0f;
            next.Play();

            StartFade(next, current, fadeSeconds);
        }

        public void Stop()
        {
            Stop(_defaultFadeSeconds);
        }

        public void Stop(float fadeSeconds)
        {
            StartFade(null, _sources[_activeIndex], fadeSeconds);
        }

        private void StartFade(AudioSource fadeIn, AudioSource fadeOut, float seconds)
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            // 페이드 도중에 곡이 다시 바뀌면 남아 있던 다른 소스가 어중간한 볼륨으로 계속 울리므로 정리한다
            for (int i = 0; i < _sources.Length; i++)
            {
                AudioSource source = _sources[i];
                if (source != fadeIn && source != fadeOut)
                {
                    source.Stop();
                    source.volume = 0f;
                }
            }

            _fadeRoutine = StartCoroutine(Crossfade(fadeIn, fadeOut, Mathf.Max(0f, seconds)));
        }

        private IEnumerator Crossfade(AudioSource fadeIn, AudioSource fadeOut, float seconds)
        {
            float startOut = fadeOut != null ? fadeOut.volume : 0f;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                // 게임오버 화면은 timeScale을 0으로 멈추므로 실제 시간으로 페이드해야 멈추지 않는다
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / seconds;

                if (fadeIn != null)
                {
                    fadeIn.volume = t;
                }

                if (fadeOut != null)
                {
                    fadeOut.volume = Mathf.Lerp(startOut, 0f, t);
                }

                yield return null;
            }

            if (fadeIn != null)
            {
                fadeIn.volume = 1f;
            }

            if (fadeOut != null)
            {
                fadeOut.volume = 0f;
                fadeOut.Stop();
            }

            _fadeRoutine = null;
        }
    }
}
