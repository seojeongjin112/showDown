using System;
using System.Collections;
using ArenaBreak.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Core
{
    public enum TutorialGoal
    {
        MoveAndLook,
        Jump,
        MeleeHits,
        EquipGun,
        GunHits,
        Reload,
        DefeatEnemy
    }

    // 시작 구역의 단계별 튜토리얼. 단계마다 안내를 띄우고 조건을 채우면 다음 단계로 넘어가며, 모두 끝내면 출구 문을 연다
    public class TutorialManager : MonoBehaviour
    {
        [Serializable]
        public class IconSet
        {
            [SerializeField] private Sprite[] _frames;
            [SerializeField] private float _framesPerSecond = 6f;
            [Tooltip("이 아이콘 옆에 붙일 짧은 글자. 비워 두면 아이콘만 보인다")]
            [SerializeField] private string _label;

            public Sprite[] Frames => _frames;
            public float FramesPerSecond => _framesPerSecond;
            public string Label => _label;
        }

        [Serializable]
        public class Step
        {
            [SerializeField] private string _title;
            [TextArea(1, 3)]
            [SerializeField] private string _message;
            [SerializeField] private TutorialGoal _goal;
            [Tooltip("조건을 채워야 하는 횟수. 이동·시선 단계는 쓰지 않는다")]
            [SerializeField] private int _target = 1;
            [SerializeField] private IconSet[] _icons;
            [Tooltip("이 단계가 시작될 때 켜는 오브젝트(허수아비 등)")]
            [SerializeField] private GameObject[] _activate;
            [Tooltip("이 단계가 시작될 때 끄는 오브젝트. 같은 자리의 허수아비를 다른 모습으로 바꿀 때 쓴다")]
            [SerializeField] private GameObject[] _deactivate;
            [Tooltip("이 단계가 시작될 때 지급하는 무기와 예비 탄약")]
            [SerializeField] private WeaponData _grantWeapon;
            [Tooltip("DefeatEnemy 단계에서 쓰러뜨려야 하는 적들")]
            [SerializeField] private Health[] _enemies;
            [Tooltip("이 단계에서 화살표로 가리킬 표적. 비워 두면 가리키지 않는다")]
            [SerializeField] private Transform _markTarget;
            [Tooltip("화살표 위에 적을 번호. 0이면 번호를 쓰지 않는다")]
            [SerializeField] private int _markNumber;

            public string Title => _title;
            public string Message => _message;
            public TutorialGoal Goal => _goal;
            public int Target => _target;
            public IconSet[] Icons => _icons;
            public GameObject[] Activate => _activate;
            public GameObject[] Deactivate => _deactivate;
            public WeaponData GrantWeapon => _grantWeapon;
            public Health[] Enemies => _enemies;
            public Transform MarkTarget => _markTarget;
            public int MarkNumber => _markNumber;
        }

        private const float MoveGoalDistance = 6f;
        private const float LookGoalDegrees = 90f;
        private const float NextStepDelaySeconds = 1f;

        [SerializeField] private Step[] _steps;
        [Tooltip("튜토리얼을 마치거나 건너뛰면 열리는 문들. 길 폭이 넓어서 문 여러 개를 나란히 둘 수 있다")]
        [SerializeField] private WaveGate[] _exitGates;
        [SerializeField] private TutorialMarker _marker;
        [Tooltip("튜토리얼이 끝나거나 건너뛸 때 치우는 오브젝트들")]
        [SerializeField] private GameObject[] _cleanup;
        [Tooltip("끄면 게임을 켤 때마다 튜토리얼이 처음부터 나온다. 켜면 한 번 끝낸 뒤에는 건너뛴다")]
        [SerializeField] private bool _rememberCompletion;

        private const string SavedKey = "TutorialDone";

        private FirstPersonMovement _movement;
        private WeaponInventory _inventory;
        private Health _playerHealth;
        private KnifeDamage[] _knives;
        private GunWeapon[] _guns;
        private int[] _lastMagazine;
        private Transform _player;
        private Vector3 _lastPosition;
        private float _lastYaw;
        private float _distance;
        private float _yaw;
        private int _stepIndex = -1;
        private int _progress;
        private bool _stepDone;
        private bool _finished;
        private bool _ignoreSelection;

        public int StepCount => _steps != null ? _steps.Length : 0;
        public int StepIndex => _stepIndex;
        public bool IsFinished => _finished;

        // HUD가 폴링하지 않고 구독해서 안내와 진행도를 그린다
        public event Action<int, Step> StepStarted;
        public event Action<int, int> ProgressChanged;
        public event Action StepCompleted;
        public event Action Finished;

        private void Awake()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null || _steps == null || _steps.Length == 0)
            {
                Debug.LogError("TutorialManager: Player 태그 오브젝트 또는 단계 목록이 비어 있습니다", this);
                enabled = false;
                return;
            }

            _player = player.transform;
            _movement = player.GetComponent<FirstPersonMovement>();
            _inventory = player.GetComponent<WeaponInventory>();
            _playerHealth = player.GetComponent<Health>();
            _knives = player.GetComponentsInChildren<KnifeDamage>(true);
            _guns = player.GetComponentsInChildren<GunWeapon>(true);
        }

        private void OnEnable()
        {
            _movement.Jumped += OnJumped;
            _inventory.SelectionChanged += OnSelectionChanged;

            for (int i = 0; i < _knives.Length; i++)
            {
                _knives[i].Hit += OnMeleeHit;
            }

            for (int i = 0; i < _guns.Length; i++)
            {
                _guns[i].Hit += OnGunHit;
            }

            _playerHealth.Changed += OnPlayerHealthChanged;
        }

        private void OnDisable()
        {
            if (_movement == null)
            {
                return;
            }

            _movement.Jumped -= OnJumped;
            _inventory.SelectionChanged -= OnSelectionChanged;

            for (int i = 0; i < _knives.Length; i++)
            {
                if (_knives[i] != null)
                {
                    _knives[i].Hit -= OnMeleeHit;
                }
            }

            for (int i = 0; i < _guns.Length; i++)
            {
                if (_guns[i] != null)
                {
                    _guns[i].Hit -= OnGunHit;
                }
            }

            _playerHealth.Changed -= OnPlayerHealthChanged;
            UnwatchAmmo();
        }

        private void Start()
        {
            if (_rememberCompletion && PlayerPrefs.GetInt(SavedKey, 0) == 1)
            {
                Finish(false);
                return;
            }

            WatchAmmo();
            BeginStep(0);
        }

        private void Update()
        {
            if (_finished)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                Finish(true);
                return;
            }

            if (_stepDone || _steps[_stepIndex].Goal != TutorialGoal.MoveAndLook)
            {
                _lastPosition = _player.position;
                _lastYaw = _player.eulerAngles.y;
                return;
            }

            // 이동 거리와 시선 회전을 둘 다 채워야 한다
            Vector3 delta = _player.position - _lastPosition;
            delta.y = 0f;
            _distance += delta.magnitude;
            _yaw += Mathf.Abs(Mathf.DeltaAngle(_lastYaw, _player.eulerAngles.y));
            _lastPosition = _player.position;
            _lastYaw = _player.eulerAngles.y;

            float ratio = Mathf.Min(_distance / MoveGoalDistance, _yaw / LookGoalDegrees);
            SetProgress(Mathf.Clamp(Mathf.RoundToInt(ratio * 100f), 0, 100), 100);
        }

        private void BeginStep(int index)
        {
            _stepIndex = index;
            _progress = 0;
            _stepDone = false;
            _distance = 0f;
            _yaw = 0f;
            _lastPosition = _player.position;
            _lastYaw = _player.eulerAngles.y;

            Step step = _steps[index];
            for (int i = 0; step.Activate != null && i < step.Activate.Length; i++)
            {
                if (step.Activate[i] != null)
                {
                    step.Activate[i].SetActive(true);
                }
            }

            for (int i = 0; step.Deactivate != null && i < step.Deactivate.Length; i++)
            {
                if (step.Deactivate[i] != null)
                {
                    step.Deactivate[i].SetActive(false);
                }
            }

            if (step.GrantWeapon != null)
            {
                Grant(step.GrantWeapon);
            }

            if (step.Goal == TutorialGoal.DefeatEnemy)
            {
                WatchEnemies(step);
            }

            if (_marker != null)
            {
                if (step.MarkTarget != null)
                {
                    _marker.Show(step.MarkTarget, step.MarkNumber);
                }
                else
                {
                    _marker.Hide();
                }
            }

            StepStarted?.Invoke(index, step);
            ProgressChanged?.Invoke(0, TargetOf(step));
        }

        private void Grant(WeaponData weapon)
        {
            // 지급하면 인벤토리가 그 무기를 바로 들어 버려서, 지급 순간의 선택 변경이 "무기를 바꿨다"로 읽히면 단계가 곧바로 끝난다.
            // 그래서 지급 중의 변화는 무시하고, 근접 무기로 되돌려 놓아 플레이어가 직접 바꾸게 한다
            _ignoreSelection = true;

            if (_inventory.CanUnlock(weapon))
            {
                _inventory.Unlock(weapon);
            }

            int melee = _inventory.OwnedIndexIn(WeaponSlotType.Melee);
            if (melee >= 0)
            {
                _inventory.Select(melee);
            }

            _ignoreSelection = false;

            if (weapon.UsesAmmo)
            {
                _inventory.AddAmmo(weapon, weapon.MaxReserve);
            }
        }

        private int TargetOf(Step step)
        {
            if (step.Goal == TutorialGoal.MoveAndLook)
            {
                return 100;
            }

            return step.Goal == TutorialGoal.DefeatEnemy && step.Enemies != null ? step.Enemies.Length : Mathf.Max(1, step.Target);
        }

        private void Advance(int amount)
        {
            if (_stepDone || _finished)
            {
                return;
            }

            SetProgress(_progress + amount, TargetOf(_steps[_stepIndex]));
        }

        private void SetProgress(int value, int target)
        {
            if (_stepDone || value == _progress)
            {
                return;
            }

            _progress = Mathf.Min(value, target);
            ProgressChanged?.Invoke(_progress, target);

            if (_progress >= target)
            {
                _stepDone = true;
                StartCoroutine(NextStepAfterDelay());
            }
        }

        // 완료 표시를 잠깐 보여 준 뒤에 다음 단계로 넘어간다
        private IEnumerator NextStepAfterDelay()
        {
            if (_marker != null)
            {
                _marker.Hide();
            }

            StepCompleted?.Invoke();
            yield return new WaitForSeconds(NextStepDelaySeconds);

            if (_stepIndex + 1 >= _steps.Length)
            {
                Finish(false);
            }
            else
            {
                BeginStep(_stepIndex + 1);
            }
        }

        private void Finish(bool skipped)
        {
            if (_finished)
            {
                return;
            }

            _finished = true;
            StopAllCoroutines();

            if (_marker != null)
            {
                _marker.Hide();
            }

            UnwatchAmmo();

            if (_rememberCompletion)
            {
                PlayerPrefs.SetInt(SavedKey, 1);
            }

            // 건너뛰면 무기 지급 단계를 거치지 않았어도 연습한 무기를 받을 수 있게 지급 무기를 모두 준다
            if (skipped)
            {
                for (int i = 0; i < _steps.Length; i++)
                {
                    if (_steps[i].GrantWeapon != null)
                    {
                        Grant(_steps[i].GrantWeapon);
                    }
                }
            }

            for (int i = 0; _cleanup != null && i < _cleanup.Length; i++)
            {
                if (_cleanup[i] != null)
                {
                    _cleanup[i].SetActive(false);
                }
            }

            for (int i = 0; _exitGates != null && i < _exitGates.Length; i++)
            {
                if (_exitGates[i] != null)
                {
                    _exitGates[i].Open();
                }
            }

            Finished?.Invoke();
        }

        private void OnJumped()
        {
            if (IsGoal(TutorialGoal.Jump))
            {
                Advance(1);
            }
        }

        private void OnMeleeHit(bool killed)
        {
            if (IsGoal(TutorialGoal.MeleeHits))
            {
                Advance(1);
            }
        }

        private void OnGunHit(bool killed)
        {
            if (IsGoal(TutorialGoal.GunHits))
            {
                Advance(1);
            }
        }

        private void OnSelectionChanged(int index)
        {
            if (_ignoreSelection || !IsGoal(TutorialGoal.EquipGun))
            {
                return;
            }

            // 총 슬롯으로 바꾸면 성공이다. 칼은 탄창이 없다
            if (_inventory.WeaponAt(index).MagazineSize > 0)
            {
                Advance(1);
            }
        }

        // 탄창이 차오른 순간이 장전이 끝난 순간이다
        private void OnAmmoChanged(int slot, int current)
        {
            bool reloaded = current > _lastMagazine[slot];
            _lastMagazine[slot] = current;

            if (reloaded && IsGoal(TutorialGoal.Reload))
            {
                Advance(1);
            }
        }

        private void WatchEnemies(Step step)
        {
            for (int i = 0; i < step.Enemies.Length; i++)
            {
                Health enemy = step.Enemies[i];
                if (enemy == null || enemy.IsDead)
                {
                    Advance(1);
                    continue;
                }

                // 적은 한 번만 죽으므로 핸들러가 스스로 구독을 해제한다
                Action onDied = null;
                onDied = () =>
                {
                    enemy.Died -= onDied;
                    Advance(1);
                };
                enemy.Died += onDied;
            }
        }

        // 튜토리얼 중에는 죽어서 처음부터 다시 하지 않도록 체력이 바닥나기 전에 채워 준다
        private void OnPlayerHealthChanged(int current, int max)
        {
            if (!_finished && current <= 2 && current > 0)
            {
                _playerHealth.Heal(max);
            }
        }

        private bool IsGoal(TutorialGoal goal)
        {
            return !_finished && !_stepDone && _stepIndex >= 0 && _steps[_stepIndex].Goal == goal;
        }

        private System.Action<int, int>[] _ammoHandlers;

        private void WatchAmmo()
        {
            int count = _inventory.Count;
            _lastMagazine = new int[count];
            _ammoHandlers = new System.Action<int, int>[count];

            for (int i = 0; i < count; i++)
            {
                WeaponAmmo ammo = _inventory.AmmoAt(i);
                if (ammo == null)
                {
                    continue;
                }

                int slot = i;
                _lastMagazine[i] = ammo.Magazine;
                _ammoHandlers[i] = (current, reserve) => OnAmmoChanged(slot, current);
                ammo.Changed += _ammoHandlers[i];
            }
        }

        private void UnwatchAmmo()
        {
            if (_ammoHandlers == null)
            {
                return;
            }

            for (int i = 0; i < _ammoHandlers.Length; i++)
            {
                WeaponAmmo ammo = _inventory.AmmoAt(i);
                if (ammo != null && _ammoHandlers[i] != null)
                {
                    ammo.Changed -= _ammoHandlers[i];
                }
            }

            _ammoHandlers = null;
        }
    }
}
