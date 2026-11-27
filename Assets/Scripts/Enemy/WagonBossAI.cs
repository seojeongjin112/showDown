using System;
using System.Collections;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace ArenaBreak.Enemy
{
    // 무법자 마차 보스의 한 단계. 돌진·탄막·다이너마이트를 순서대로 쓰고, 부서진 단계(움직이지 않음)에서는 탄막과 다이너마이트만 쓴다.
    // 단계 전환과 체력은 BossEncounter가 맡으므로 이 AI는 자기 단계의 전투만 한다.
    // 사망 때 EnemyDeath가 공격 구현을 꺼서 멈추도록 IEnemyAttack을 구현하지만, 실제 공격은 아래 코루틴이 한다
    [RequireComponent(typeof(NavMeshAgent), typeof(Health), typeof(BossSpriteAnimator))]
    public class WagonBossAI : MonoBehaviour, IEnemyAttack
    {
        private enum Pattern
        {
            Charge,
            Barrage,
            Dynamite,
            LongBarrage
        }

        private const string PlayerTag = "Player";

        [Header("공통")]
        [SerializeField] private Material _telegraphMaterial;
        [SerializeField] private Pattern[] _patterns = { Pattern.Barrage, Pattern.Charge, Pattern.Dynamite };
        [Tooltip("끄면 제자리에서 싸운다. 부서진 마차용")]
        [SerializeField] private bool _mobile = true;
        [SerializeField] private float _moveSpeed = 3.5f;
        [Tooltip("패턴 사이에 쫓아다니거나 기다리는 시간")]
        [SerializeField] private float _idleSeconds = 2.5f;
        [SerializeField] private float _repathInterval = 0.25f;
        [Tooltip("켜면 등장할 때 등장 모션을 재생한다. 첫 단계만 켠다")]
        [SerializeField] private bool _playIntro;
        [SerializeField] private BossWarningIcon _warning;

        [Header("돌진")]
        [SerializeField] private float _chargeWindup = 1.2f;
        [SerializeField] private float _chargeSpeed = 16f;
        [SerializeField] private float _chargeDistance = 24f;
        [SerializeField] private float _chargeHitRadius = 2.6f;
        [SerializeField] private int _chargeDamage = 4;
        [SerializeField] private float _chargeRecoverSeconds = 1f;

        [Header("탄막")]
        [SerializeField] private EnemyProjectile _projectilePrefab;
        [SerializeField] private float _barrageWindup = 0.8f;
        [SerializeField] private int _barrageShots = 14;
        [SerializeField] private float _shotInterval = 0.1f;
        [Tooltip("플레이어를 가운데로 이 각도만큼 좌우로 훑으며 쏜다")]
        [SerializeField] private float _fanDegrees = 50f;
        [SerializeField] private int _shotDamage = 1;
        [SerializeField] private float _muzzleHeight = 2f;
        [Tooltip("플레이어 발밑 기준으로 조준하는 높이. 눈높이 근처로 날아가야 맞는다")]
        [SerializeField] private float _aimHeight = 1.2f;

        [Header("다이너마이트")]
        [SerializeField] private EnemyBomb _bombPrefab;
        [SerializeField] private float _bombWindup = 0.6f;
        [SerializeField] private int _bombCount = 3;
        [SerializeField] private float _bombInterval = 0.45f;
        [Tooltip("첫 개는 플레이어 자리에, 나머지는 이 반경 안에 흩뿌린다")]
        [SerializeField] private float _bombScatter = 4f;
        [SerializeField] private float _throwHeight = 2.2f;
        [SerializeField] private float _flightTime = 1.2f;
        [SerializeField] private int _bombDamage = 3;

        [Header("긴 탄막 (부서진 마차)")]
        [Tooltip("총알과 폭탄이 이 시간 동안 쉬지 않고 이어진다. 움직여서 피해야 한다")]
        [SerializeField] private float _longBarrageSeconds = 8f;
        [Tooltip("시작 전에 경고 표시가 뜨는 시간")]
        [SerializeField] private float _longBarrageWindup = 1.2f;
        [SerializeField] private float _longShotInterval = 0.45f;
        [SerializeField] private float _longBombInterval = 2f;
        [SerializeField] private int _longBombDamage = 2;

        private NavMeshAgent _agent;
        private Health _health;
        private BossSpriteAnimator _animator;
        private Transform _target;
        private IDamageable _targetDamageable;
        private int _patternIndex;

        public float Range => 0f;
        public bool IsReady => false;

        // 패턴이 시작될 때 알린다. 소리나 연출이 구독할 수 있게 열어 둔다
        public event Action<Transform> Attacked;

        private void Awake()
        {
            // 난이도가 높을수록 패턴 사이에 쉬는 시간이 짧다
            _idleSeconds *= Difficulty.BossPatternInterval;
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _animator = GetComponent<BossSpriteAnimator>();

            GameObject player = GameObject.FindWithTag(PlayerTag);
            if (player == null || _patterns == null || _patterns.Length == 0)
            {
                Debug.LogError("WagonBossAI: Player 태그 오브젝트 또는 패턴 목록이 비어 있습니다", this);
                enabled = false;
                return;
            }

            _target = player.transform;
            _targetDamageable = player.GetComponent<IDamageable>();

            // 방향은 YawBillboard가 정하므로 에이전트가 몸을 돌리면 스프라이트가 흔들린다
            _agent.updateRotation = false;
            _agent.speed = _moveSpeed;
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
        }

        private void Start()
        {
            StartCoroutine(Run());
        }

        public void Execute(Transform target)
        {
        }

        // 사망하면 진행 중이던 패턴이 시체에서 피해를 주지 않도록 코루틴을 모두 끊는다
        private void OnDied()
        {
            StopAllCoroutines();

            if (_warning != null)
            {
                _warning.Hide();
            }
        }

        private IEnumerator Run()
        {
            // 스폰 직후 NavMesh에 올라앉을 때까지 한 프레임 기다린다
            yield return null;

            if (_playIntro)
            {
                _animator.Play(BossClip.Roar);
                yield return new WaitForSeconds(_animator.Duration(BossClip.Roar));
            }

            while (true)
            {
                yield return Idle(_idleSeconds);

                Pattern pattern = _patterns[_patternIndex % _patterns.Length];
                _patternIndex++;
                Attacked?.Invoke(_target);

                switch (pattern)
                {
                    case Pattern.Charge:
                        yield return Charge();
                        break;
                    case Pattern.Barrage:
                        yield return Barrage();
                        break;
                    case Pattern.LongBarrage:
                        yield return LongBarrage();
                        break;
                    default:
                        yield return Dynamite();
                        break;
                }

                _animator.Play(BossClip.Walk);
            }
        }

        private IEnumerator Idle(float seconds)
        {
            _animator.Play(BossClip.Walk);

            if (!_mobile)
            {
                yield return new WaitForSeconds(seconds);
                yield break;
            }

            _agent.isStopped = false;
            float end = Time.time + seconds;
            float nextRepath = 0f;

            while (Time.time < end)
            {
                if (_agent.isOnNavMesh && Time.time >= nextRepath)
                {
                    nextRepath = Time.time + _repathInterval;
                    _agent.SetDestination(_target.position);
                }

                yield return null;
            }
        }

        private IEnumerator Charge()
        {
            Stop();
            Vector3 direction = _target.position - transform.position;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward;

            float distance = Mathf.Min(_chargeDistance, FlatDistance(_target.position) + 5f);
            Vector3 destination = transform.position + direction * distance;

            // 도착 지점을 먼저 보여 줘서 피할 시간을 준다
            ShowTelegraph(destination, _chargeHitRadius * 1.3f, _chargeWindup);
            if (_warning != null)
            {
                _warning.Show(_chargeWindup);
            }

            yield return new WaitForSeconds(_chargeWindup);

            _animator.Play(BossClip.Charge);
            bool hit = false;
            float travelled = 0f;

            while (travelled < distance)
            {
                float step = _chargeSpeed * Time.deltaTime;
                _agent.Move(direction * step);
                travelled += step;

                if (!hit && FlatDistance(_target.position) <= _chargeHitRadius)
                {
                    hit = true;
                    Hurt(_chargeDamage);
                }

                yield return null;
            }

            yield return new WaitForSeconds(_chargeRecoverSeconds);
        }

        // 플레이어를 가운데로 부채꼴을 훑으며 연달아 쏜다. 한 곳에 서 있으면 맞고, 옆으로 빠지면 피한다
        private IEnumerator Barrage()
        {
            Stop();
            _animator.Play(BossClip.Barrage);
            if (_warning != null)
            {
                _warning.Show(_barrageWindup);
            }

            yield return new WaitForSeconds(_barrageWindup);

            if (_projectilePrefab == null)
            {
                yield break;
            }

            Vector3 aim = _target.position - transform.position;
            aim.y = 0f;
            Vector3 center = aim.sqrMagnitude > 0.01f ? aim.normalized : transform.forward;

            for (int i = 0; i < _barrageShots; i++)
            {
                float t = _barrageShots > 1 ? (float)i / (_barrageShots - 1) : 0.5f;
                float angle = Mathf.Lerp(-_fanDegrees * 0.5f, _fanDegrees * 0.5f, t);
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * center;

                Vector3 muzzle = transform.position + Vector3.up * _muzzleHeight + direction * 1.5f;
                // 탄은 플레이어의 눈높이 쪽으로 날아가야 닿으므로 높이를 조준 높이에 맞춘다
                Vector3 aimPoint = new Vector3(muzzle.x + direction.x * 10f, _target.position.y + _aimHeight, muzzle.z + direction.z * 10f);

                EnemyProjectile projectile = Instantiate(_projectilePrefab, muzzle, Quaternion.identity);
                projectile.Launch(aimPoint - muzzle, _target, _aimHeight, _shotDamage);

                yield return new WaitForSeconds(_shotInterval);
            }

            yield return new WaitForSeconds(0.4f);
        }

        // 플레이어 자리와 그 주변에 다이너마이트를 연달아 던진다. 떨어질 자리에는 폭발 반경 원이 먼저 뜬다
        private IEnumerator Dynamite()
        {
            Stop();
            _animator.Play(BossClip.DynamiteThrow);
            if (_warning != null)
            {
                _warning.Show(_bombWindup);
            }

            yield return new WaitForSeconds(_bombWindup);

            if (_bombPrefab == null)
            {
                yield break;
            }

            for (int i = 0; i < _bombCount; i++)
            {
                Vector2 scatter = i == 0 ? Vector2.zero : Random.insideUnitCircle * _bombScatter;
                Vector3 landing = _target.position + new Vector3(scatter.x, 0f, scatter.y);
                landing.y = _target.position.y;

                Vector3 origin = transform.position + Vector3.up * _throwHeight;
                EnemyBomb bomb = Instantiate(_bombPrefab, origin, Quaternion.identity);
                bomb.Launch(CalculateVelocity(origin, landing), landing.y, _target, _bombDamage);

                yield return new WaitForSeconds(_bombInterval);
            }

            yield return new WaitForSeconds(0.5f);
        }

        // 총알과 폭탄이 쉬지 않고 길게 이어진다. 한곳에 서 있으면 맞으니 계속 움직여야 한다
        private IEnumerator LongBarrage()
        {
            Stop();
            _animator.Play(BossClip.Barrage);
            if (_warning != null)
            {
                _warning.Show(_longBarrageWindup);
            }

            yield return new WaitForSeconds(_longBarrageWindup);
            yield return FireLongBarrage(_longBarrageSeconds);
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator FireLongBarrage(float seconds)
        {
            float end = Time.time + seconds;
            float nextShot = 0f;
            float nextBomb = 0f;

            while (Time.time < end)
            {
                if (_projectilePrefab != null && Time.time >= nextShot)
                {
                    nextShot = Time.time + _longShotInterval;
                    Vector3 toPlayer = _target.position - transform.position;
                    toPlayer.y = 0f;
                    Vector3 direction = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : transform.forward;
                    // 정확히 겨누면 가만히 있기만 해도 맞아서 조금씩 흩어 쏜다
                    direction = Quaternion.Euler(0f, Random.Range(-8f, 8f), 0f) * direction;

                    Vector3 muzzle = transform.position + Vector3.up * _muzzleHeight + direction * 1.5f;
                    Vector3 aimPoint = new Vector3(muzzle.x + direction.x * 10f, _target.position.y + _aimHeight, muzzle.z + direction.z * 10f);
                    EnemyProjectile projectile = Instantiate(_projectilePrefab, muzzle, Quaternion.identity);
                    projectile.Launch(aimPoint - muzzle, _target, _aimHeight, _shotDamage);
                }

                if (_bombPrefab != null && Time.time >= nextBomb)
                {
                    nextBomb = Time.time + _longBombInterval;
                    Vector2 scatter = Random.insideUnitCircle * (_bombScatter * 0.5f);
                    Vector3 landing = _target.position + new Vector3(scatter.x, 0f, scatter.y);
                    landing.y = _target.position.y;

                    Vector3 origin = transform.position + Vector3.up * _throwHeight;
                    EnemyBomb bomb = Instantiate(_bombPrefab, origin, Quaternion.identity);
                    bomb.Launch(CalculateVelocity(origin, landing), landing.y, _target, _longBombDamage);
                }

                yield return null;
            }
        }

        // 비행 시간을 고정하면 거리와 상관없이 항상 목표 지점에 정확히 떨어지는 초기 속도가 나온다
        private Vector3 CalculateVelocity(Vector3 origin, Vector3 landing)
        {
            Vector3 displacement = landing - origin;
            Vector3 horizontal = new Vector3(displacement.x, 0f, displacement.z) / _flightTime;
            float vertical = (displacement.y - 0.5f * Physics.gravity.y * _flightTime * _flightTime) / _flightTime;
            return horizontal + Vector3.up * vertical;
        }

        private void ShowTelegraph(Vector3 point, float radius, float seconds)
        {
            if (_telegraphMaterial != null)
            {
                BombBlastMarker.Spawn(_telegraphMaterial, new Vector3(point.x, transform.position.y, point.z), radius, seconds);
            }
        }

        private void Stop()
        {
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        private void Hurt(int damage)
        {
            _targetDamageable?.TakeDamage(damage);
        }

        private float FlatDistance(Vector3 point)
        {
            Vector3 offset = point - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }
    }
}
