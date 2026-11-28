using UnityEngine;

namespace ArenaBreak.Core
{
    // 폭발 이펙트·폭탄처럼 생성되는 순간 소리가 나야 하는 프리팹에 붙인다.
    // 다이너마이트와 적 폭탄이 같은 폭발 이펙트를 쓰므로 이펙트에 붙이면 두 스크립트를 고치지 않아도 된다
    public class SpawnSound : MonoBehaviour
    {
        [Tooltip("여러 개를 넣으면 생성될 때마다 하나를 골라 같은 폭발음이 반복되지 않게 한다")]
        [SerializeField] private AudioClip[] _clips;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        private void Start()
        {
            if (_clips == null || _clips.Length == 0)
            {
                return;
            }

            SfxPlayer.PlayAt(_clips[Random.Range(0, _clips.Length)], transform.position, _volume);
        }
    }
}
