using UnityEngine;

namespace ArenaBreak.Core
{
    // 마을 바깥으로 도망쳐 적을 피하는 플레이를 막기 위한 구역이라 플레이어에게만 피해를 준다
    public class DangerZone : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        // 이 오브젝트 위치를 중심으로 한 정사각형의 반변 길이. 그 바깥이 위험 구역이다
        [SerializeField] private float _safeHalfExtent = 48f;
        [SerializeField] private int _damagePerTick = 1;
        [SerializeField] private float _tickInterval = 1f;

        private Transform _player;
        private IDamageable _damageable;
        private float _elapsedOutside;

        private void Awake()
        {
            GameObject player = GameObject.FindWithTag(PlayerTag);
            if (player == null)
            {
                Debug.LogError("DangerZone: Player 태그 오브젝트를 찾지 못했습니다", this);
                enabled = false;
                return;
            }

            _player = player.transform;
            _damageable = player.GetComponent<IDamageable>();
            if (_damageable == null)
            {
                Debug.LogError("DangerZone: 플레이어에 IDamageable 구현이 없습니다", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (IsInsideSafeArea(_player.position))
            {
                // 잠깐 걸쳤다 돌아올 때 이전 누적 시간 때문에 곧바로 맞지 않게 초기화한다
                _elapsedOutside = 0f;
                return;
            }

            _elapsedOutside += Time.deltaTime;
            while (_elapsedOutside >= _tickInterval)
            {
                _elapsedOutside -= _tickInterval;
                _damageable.TakeDamage(_damagePerTick);
            }
        }

        private bool IsInsideSafeArea(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            return Mathf.Abs(offset.x) <= _safeHalfExtent && Mathf.Abs(offset.z) <= _safeHalfExtent;
        }
    }
}
