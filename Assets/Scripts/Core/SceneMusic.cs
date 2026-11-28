using UnityEngine;

namespace ArenaBreak.Core
{
    // 씬마다 기본 곡을 정해 두고, 어떤 씬에서 플레이를 시작해도 음악 관리자가 생기도록 한다
    public class SceneMusic : MonoBehaviour
    {
        [SerializeField] private MusicManager _managerPrefab;
        [Tooltip("비워 두면 이 씬에 들어올 때 음악을 끈다")]
        [SerializeField] private AudioClip _music;

        // StageMusic 같은 다른 음악 선택이 Start에서 곡을 바꾸므로, 그보다 먼저 관리자를 만들고 기본 곡을 걸어 두려고 Awake에서 한다
        private void Awake()
        {
            MusicManager manager = MusicManager.Instance;
            if (manager == null)
            {
                if (_managerPrefab == null)
                {
                    Debug.LogError("SceneMusic: 음악 관리자 프리팹이 연결되지 않았습니다", this);
                    return;
                }

                manager = Instantiate(_managerPrefab);
            }

            manager.Play(_music);
        }
    }
}
