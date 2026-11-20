using System;
using UnityEngine;

namespace ArenaBreak.Enemy
{
    public class BombThrowAttack : MonoBehaviour, IEnemyAttack
    {
        [SerializeField] private EnemyBomb _bombPrefab;
        [SerializeField] private float _range = 12f;
        [SerializeField] private float _cooldown = 3f;
        [SerializeField] private float _throwHeight = 1.2f;
        [SerializeField] private float _flightTime = 1.2f;
        [SerializeField] private int _damage = 3;

        private float _nextAttackTime;

        public float Range => _range;
        public bool IsReady => Time.time >= _nextAttackTime;

        public event Action<Transform> Attacked;

        private void Awake()
        {
            if (_bombPrefab == null)
            {
                Debug.LogError("BombThrowAttack: 폭탄 프리팹 참조가 비어 있습니다", this);
                enabled = false;
            }
        }

        public void Execute(Transform target)
        {
            _nextAttackTime = Time.time + _cooldown;

            Vector3 origin = transform.position + Vector3.up * _throwHeight;
            Vector3 landing = target.position;

            EnemyBomb bomb = Instantiate(_bombPrefab, origin, Quaternion.identity);
            bomb.Launch(CalculateVelocity(origin, landing), landing.y, target, _damage);

            Attacked?.Invoke(target);
        }

        // 비행 시간을 고정하면 거리와 상관없이 항상 목표 지점에 정확히 떨어지는 초기 속도가 나온다
        private Vector3 CalculateVelocity(Vector3 origin, Vector3 landing)
        {
            Vector3 displacement = landing - origin;
            Vector3 horizontal = new Vector3(displacement.x, 0f, displacement.z) / _flightTime;
            float vertical = (displacement.y - 0.5f * Physics.gravity.y * _flightTime * _flightTime) / _flightTime;
            return horizontal + Vector3.up * vertical;
        }
    }
}
