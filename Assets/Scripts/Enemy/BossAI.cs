using System;
using System.Collections;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace ArenaBreak.Enemy
{
    // 추격하다가 일정 간격마다 강타 → 돌진 → 소환을 순서대로 쓰는 보스. 체력이 기준 아래로 내려가면 한 번 분노해 더 빠르고 자주 공격한다.
    // 사망 때 EnemyDeath가 공격 구현을 꺼서 멈추도록 IEnemyAttack을 구현하지만, 실제 공격은 아래 코루틴이 한다
    [RequireComponent(typeof(NavMeshAgent), typeof(Health), typeof(BossSpriteAnimator))]
    public class BossAI : MonoBehaviour, IEnemyAttack
    {
        private enum Pattern
        {
            Slam,
            Charge,
            Summon,
            Lightning
        }

        private const string PlayerTag = "Player";

        [Header("공통")]
        [SerializeField] private Material _telegraphMaterial;
        [SerializeField] private Pattern[] _patterns = { Pattern.Slam, Pattern.Charge, Pattern.Summon };
        [SerializeField] private float _moveSpeed = 2.6f;
        [Tooltip("추격만 하는 시간. 이 시간이 지나면 다음 패턴을 쓴다")]
        [SerializeField] private float _chaseSeconds = 3f;
        [SerializeField] private float _repathInterval = 0.25f;

        [Header("근접 타격 (추격 중 가까워졌을 때)")]
        [SerializeField] private float _meleeRange = 3f;
        [SerializeField] private float _meleeCooldown = 2.4f;
        [SerializeField] private int _meleeDamage = 2;
        [Tooltip("타격 순간에 이만큼 더 멀리 있어도 맞는다. 모션 중 살짝 물러난 경우를 봐준다")]
        [SerializeField] private float _hitMargin = 0.8f;

        [Header("강타")]
        [SerializeField] private float _slamRadius = 6f;
        [SerializeField] private float _slamWindup = 1.2f;
        [SerializeField] private int _slamDamage = 4;
        [Tooltip("플레이어가 이보다 멀면 강타 차례에도 돌진을 쓴다")]
        [SerializeField] private float _slamEngageDistance = 10f;

        [Header("돌진")]
        [SerializeField] private float _chargeWindup = 1f;
        [SerializeField] private float _chargeSpeed = 14f;
        [SerializeField] private float _chargeDistance = 20f;
        [SerializeField] private float _chargeHitRadius = 2f;
        [SerializeField] private int _chargeDamage = 3;
        [SerializeField] private float _chargeRecoverSeconds = 0.8f;

        [Header("소환")]
        [SerializeField] private GameObject _minionPrefab;
        [SerializeField] private int _minionCount = 3;
        [SerializeField] private float _summonRadius = 5f;
        [Tooltip("살아 있는 졸개가 이만큼 이상이면 소환을 건너뛴다")]
        [SerializeField] private int _maxMinions = 8;

        [Header("번개")]
        [SerializeField] private Sprite[] _lightningStrike;
        [SerializeField] private Sprite[] _lightningImpact;
        [SerializeField] private int _lightningStrikes = 4;
        [Tooltip("번개가 떨어질 자리에 원이 뜬 뒤 내리꽂히기까지의 시간")]
        [SerializeField] private float _lightningWarning = 1.2f;
        [Tooltip("줄기마다 이만큼씩 늦게 떨어져 한꺼번에 피할 수 없게 한다")]
        [SerializeField] private float _lightningInterval = 0.4f;
        [SerializeField] private float _lightningRadius = 2.2f;
        [Tooltip("첫 줄기는 플레이어 자리에, 나머지는 이 반경 안에 흩뿌린다")]
        [SerializeField] private float _lightningScatter = 5f;
        [SerializeField] private int _lightningDamage = 3;
        [SerializeField] private float _lightningScale = 5f;
        [SerializeField] private float _lightningFramesPerSecond = 24f;
        [SerializeField] private int _enragedExtraStrikes = 3;

        [Header("공격 경고 표시")]
        [Tooltip("공격 준비 동안 머리 위에 띄울 느낌표. 비워 두면 표시하지 않는다")]
        [SerializeField] private SpriteRenderer _warningIcon;
        [Tooltip("보스 몸통 가운데에서 이만큼 위(+)나 아래(-)에 띄운다")]
        [SerializeField] private float _warningHeightOffset;
        [SerializeField] private Color _warningEarlyColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color _warningLateColor = new Color(1f, 0.15f, 0.1f, 1f);
        [SerializeField] private float _warningStartScale = 2.2f;
        [SerializeField] private float _warningEndScale = 3.4f;
        [Tooltip("예고 끝 무렵에 좌우로 떠는 폭(m)")]
        [SerializeField] private float _warningShake = 0.15f;

        [Header("분노")]
        [SerializeField, Range(0f, 1f)] private float _enrageRatio = 0.5f;
        [SerializeField] private float _enragedSpeedMultiplier = 1.4f;
        [Tooltip("분노하면 추격 시간에 곱한다. 1보다 작을수록 패턴이 잦다")]
        [SerializeField] private float _enragedChaseMultiplier = 0.6f;
        [SerializeField] private int _enragedExtraMinions = 2;
        [SerializeField] private float _enragedSlamRadiusBonus = 1.5f;

        private NavMeshAgent _agent;
        private Health _health;
        private BossSpriteAnimator _animator;
        private Transform _target;
        private IDamageable _targetDamageable;
        private int _patternIndex;
        private int _aliveMinions;
        private float _nextMeleeTime;
        private bool _enraged;
        private bool _enrageQueued;
        private Vector3 _warningRestPosition;
        private float _warningStart;
        private float _warningSeconds;

        public float Range => _meleeRange;
        public bool IsReady => false;

        // 패턴이 시작될 때 알린다. 소리나 연출이 구독할 수 있게 열어 둔다
        public event Action<Transform> Attacked;

        private void Awake()
        {
            // 난이도가 높을수록 패턴 사이에 쉬는 시간이 짧다
            _chaseSeconds *= Difficulty.BossPatternInterval;
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _animator = GetComponent<BossSpriteAnimator>();

            GameObject player = GameObject.FindWithTag(PlayerTag);
            if (player == null || _patterns == null || _patterns.Length == 0)
            {
                Debug.LogError("BossAI: Player 태그 오브젝트 또는 패턴 목록이 비어 있습니다", this);
                enabled = false;
                return;
            }

            _target = player.transform;
            _targetDamageable = player.GetComponent<IDamageable>();

            // 방향은 YawBillboard가 정하므로 에이전트가 몸을 돌리면 스프라이트가 흔들린다
            _agent.updateRotation = false;
            _agent.speed = _moveSpeed;

            if (_warningIcon != null)
            {
                // 몸통 가운데 높이는 생성 시점 그림 경계에서 한 번만 잰다. 머리 위는 바라볼 때 화면 밖으로 잘리기 쉽다
                float middle = 2f;
                foreach (SpriteRenderer candidate in GetComponentsInChildren<SpriteRenderer>())
                {
                    if (candidate != _warningIcon && candidate.sprite != null)
                    {
                        middle = candidate.bounds.center.y - transform.position.y;
                        break;
                    }
                }

                _warningRestPosition = new Vector3(0f, middle + _warningHeightOffset, 0f);
                _warningIcon.transform.localPosition = _warningRestPosition;
                _warningIcon.enabled = false;
            }
        }

        private void Update()
        {
            if (_warningSeconds <= 0f)
            {
                return;
            }

            float progress = (Time.time - _warningStart) / _warningSeconds;
            if (progress >= 1f)
            {
                HideWarning();
                return;
            }

            _warningIcon.enabled = true;
            _warningIcon.color = Color.Lerp(_warningEarlyColor, _warningLateColor, progress);
            float scale = Mathf.Lerp(_warningStartScale, _warningEndScale, progress);
            _warningIcon.transform.localScale = new Vector3(scale, scale, 1f);

            // 끝날 무렵에만 떨려서 처음에는 차분하다가 급해지는 느낌을 준다
            float shake = progress > 0.6f ? Mathf.Sin(Time.time * 30f * Mathf.PI * 2f) * _warningShake * progress : 0f;
            _warningIcon.transform.localPosition = _warningRestPosition + new Vector3(shake, 0f, 0f);
        }

        private void StartWarning(float seconds)
        {
            if (_warningIcon != null)
            {
                _warningStart = Time.time;
                _warningSeconds = seconds;
            }
        }

        private void HideWarning()
        {
            _warningSeconds = 0f;
            if (_warningIcon != null)
            {
                _warningIcon.enabled = false;
            }
        }

        private void OnEnable()
        {
            _health.Changed += OnHealthChanged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Changed -= OnHealthChanged;
            _health.Died -= OnDied;
        }

        private void Start()
        {
            StartCoroutine(Run());
        }

        public void Execute(Transform target)
        {
        }

        private void OnHealthChanged(int current, int max)
        {
            if (!_enraged && max > 0 && (float)current / max <= _enrageRatio && current > 0)
            {
                _enrageQueued = true;
            }
        }

        // 사망하면 진행 중이던 패턴이 시체에서 피해를 주지 않도록 코루틴을 모두 끊는다
        private void OnDied()
        {
            StopAllCoroutines();
            HideWarning();
            StormSky.SetStorm(false);
        }

        private IEnumerator Run()
        {
            // 스폰 직후 NavMesh에 올라앉을 때까지 한 프레임 기다린다
            yield return null;

            _animator.Play(BossClip.Roar);
            yield return new WaitForSeconds(_animator.Duration(BossClip.Roar));

            while (true)
            {
                yield return Chase(_enraged ? _chaseSeconds * _enragedChaseMultiplier : _chaseSeconds);

                if (_enrageQueued)
                {
                    yield return Enrage();
                }

                Pattern pattern = _patterns[_patternIndex % _patterns.Length];
                _patternIndex++;

                // 멀리 있는 플레이어에게 강타를 쓰면 헛방이라 돌진으로 거리를 좁힌다
                if (pattern == Pattern.Slam && FlatDistance(_target.position) > _slamEngageDistance)
                {
                    pattern = Pattern.Charge;
                }

                Attacked?.Invoke(_target);

                switch (pattern)
                {
                    case Pattern.Slam:
                        yield return Slam();
                        break;
                    case Pattern.Charge:
                        yield return Charge();
                        break;
                    case Pattern.Lightning:
                        yield return Lightning();
                        break;
                    default:
                        yield return Summon();
                        break;
                }

                _animator.Play(BossClip.Walk);
            }
        }

        private IEnumerator Chase(float seconds)
        {
            _animator.Play(BossClip.Walk);
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

                if (FlatDistance(_target.position) <= _meleeRange && Time.time >= _nextMeleeTime)
                {
                    yield return Melee();
                    _animator.Play(BossClip.Walk);
                    _agent.isStopped = false;
                }

                yield return null;
            }
        }

        private IEnumerator Melee()
        {
            Stop();
            float duration = _animator.Duration(BossClip.Attack);
            _animator.Play(BossClip.Attack);
            StartWarning(duration * 0.5f);

            // 칼을 휘두르는 모션의 중간이 타격 순간이다
            yield return new WaitForSeconds(duration * 0.5f);
            if (FlatDistance(_target.position) <= _meleeRange + _hitMargin)
            {
                Hurt(_meleeDamage);
            }

            yield return new WaitForSeconds(duration * 0.5f);
            _nextMeleeTime = Time.time + _meleeCooldown;
        }

        private IEnumerator Slam()
        {
            Stop();
            float radius = _enraged ? _slamRadius + _enragedSlamRadiusBonus : _slamRadius;
            float clip = _animator.Duration(BossClip.Slam);
            // 내려치는 순간(모션의 70% 지점)이 예고가 끝나는 때와 맞도록 모션을 늦게 시작한다
            float lead = Mathf.Max(0f, _slamWindup - clip * 0.7f);

            ShowTelegraph(transform.position, radius, _slamWindup);
            StartWarning(_slamWindup);
            yield return new WaitForSeconds(lead);
            _animator.Play(BossClip.Slam);
            yield return new WaitForSeconds(_slamWindup - lead);

            if (FlatDistance(_target.position) <= radius)
            {
                Hurt(_slamDamage);
            }

            yield return new WaitForSeconds(Mathf.Max(0f, clip * 0.3f));
        }

        private IEnumerator Charge()
        {
            Stop();
            Vector3 direction = _target.position - transform.position;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward;

            float distance = Mathf.Min(_chargeDistance, FlatDistance(_target.position) + 4f);
            Vector3 destination = transform.position + direction * distance;

            // 도착 지점을 먼저 보여 줘서 피할 시간을 준다
            ShowTelegraph(destination, _chargeHitRadius * 1.5f, _chargeWindup);
            StartWarning(_chargeWindup);
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

        private IEnumerator Summon()
        {
            Stop();
            float duration = _animator.Duration(BossClip.Summon);
            _animator.Play(BossClip.Summon);
            yield return new WaitForSeconds(duration * 0.5f);

            if (_minionPrefab != null && _aliveMinions < _maxMinions)
            {
                int count = _minionCount + (_enraged ? _enragedExtraMinions : 0);
                for (int i = 0; i < count; i++)
                {
                    SpawnMinion(i, count);
                }
            }

            yield return new WaitForSeconds(duration * 0.5f);
        }

        // 하늘에서 번개가 여러 줄기 내리꽂힌다. 떨어질 자리에 원이 먼저 뜨고, 줄기마다 조금씩 늦게 떨어진다
        private IEnumerator Lightning()
        {
            Stop();
            _animator.Play(BossClip.Roar);
            StormSky.SetStorm(true);

            int strikes = _lightningStrikes + (_enraged ? _enragedExtraStrikes : 0);
            float lastDelay = 0f;
            for (int i = 0; i < strikes; i++)
            {
                // 첫 줄기는 지금 서 있는 자리를 노리고, 나머지는 주변에 흩뿌려서 한 자리에 머물지 못하게 한다
                Vector2 scatter = i == 0 ? Vector2.zero : Random.insideUnitCircle * _lightningScatter;
                Vector3 point = _target.position + new Vector3(scatter.x, 0f, scatter.y);
                point.y = _target.position.y;

                float delay = i * _lightningInterval;
                lastDelay = delay;
                StartCoroutine(Strike(point, delay));
            }

            StartWarning(_lightningWarning);
            yield return new WaitForSeconds(_lightningWarning + lastDelay + 0.5f);
            StormSky.SetStorm(false);
        }

        private IEnumerator Strike(Vector3 point, float delay)
        {
            yield return new WaitForSeconds(delay);

            ShowTelegraph(point, _lightningRadius, _lightningWarning);
            yield return new WaitForSeconds(_lightningWarning);

            LightningStrikeEffect.Spawn(_telegraphMaterial, _lightningStrike, _lightningImpact, point, _lightningScale, _lightningFramesPerSecond);
            ArenaLighting.Flash(point + Vector3.up * 2f, new Color(0.6f, 0.8f, 1f), 3.5f, _lightningRadius * 4f, 0.25f);

            Vector3 offset = _target.position - point;
            offset.y = 0f;
            if (offset.magnitude <= _lightningRadius)
            {
                Hurt(_lightningDamage);
            }
        }

        private IEnumerator Enrage()
        {
            _enrageQueued = false;
            _enraged = true;
            Stop();
            _animator.Play(BossClip.Enrage);
            _agent.speed = _moveSpeed * _enragedSpeedMultiplier;
            yield return new WaitForSeconds(_animator.Duration(BossClip.Enrage));
            _animator.Play(BossClip.Walk);
        }

        private void SpawnMinion(int index, int count)
        {
            // 보스 둘레에 고르게 세운다. NavMesh 밖이면 졸개가 움직이지 못해 가장 가까운 올바른 자리로 옮긴다
            float angle = (index + Random.value * 0.5f) / count * Mathf.PI * 2f;
            Vector3 wanted = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _summonRadius;
            Vector3 position = NavMesh.SamplePosition(wanted, out NavMeshHit hit, 4f, NavMesh.AllAreas) ? hit.position : transform.position;

            GameObject minion = Instantiate(_minionPrefab, position, Quaternion.identity);
            if (!minion.TryGetComponent(out Health health))
            {
                return;
            }

            _aliveMinions++;

            // 졸개는 한 번만 죽으므로 핸들러가 스스로 구독을 해제한다
            Action onDied = null;
            onDied = () =>
            {
                health.Died -= onDied;
                _aliveMinions--;
            };
            health.Died += onDied;
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
