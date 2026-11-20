using System;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    public class MeleeAttack : MonoBehaviour, IEnemyAttack
    {
        [SerializeField] private float _range = 1.5f;
        [SerializeField] private float _cooldown = 1f;

        private float _nextAttackTime;

        public float Range => _range;
        public bool IsReady => Time.time >= _nextAttackTime;

        // Health가 아직 없어서 피해는 여기서 주지 않고, 구독자(피해 처리·애니메이션)가 이어받게 한다
        public event Action<Transform> Attacked;

        public void Execute(Transform target)
        {
            _nextAttackTime = Time.time + _cooldown;
            Attacked?.Invoke(target);
        }
    }
}
