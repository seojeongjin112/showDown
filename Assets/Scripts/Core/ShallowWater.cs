using ArenaBreak.Player;
using UnityEngine;

namespace ArenaBreak.Core
{
    [RequireComponent(typeof(Collider))]
    public class ShallowWater : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [Range(0.1f, 1f)]
        [SerializeField] private float _speedMultiplier = 0.5f;

        private FirstPersonMovement _slowedPlayer;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(PlayerTag) || !other.TryGetComponent(out FirstPersonMovement movement))
            {
                return;
            }

            _slowedPlayer = movement;
            _slowedPlayer.SpeedMultiplier = _speedMultiplier;
        }

        private void OnTriggerExit(Collider other)
        {
            Restore();
        }

        // 물이 꺼지거나 파괴될 때 Exit가 오지 않아 플레이어가 계속 느려지는 것을 막는다
        private void OnDisable()
        {
            Restore();
        }

        private void Restore()
        {
            if (_slowedPlayer == null)
            {
                return;
            }

            _slowedPlayer.SpeedMultiplier = 1f;
            _slowedPlayer = null;
        }
    }
}
