using System.Text;
using ArenaBreak.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Core
{
    // 맵과 적을 시험할 때 처음부터 걷지 않도록 순간이동·무적·전체 처치를 키 하나로 쓴다. 에디터와 개발 빌드에서만 동작한다
    public class DebugCheats : MonoBehaviour
    {
        private const string PlayerTag = "Player";
        private const int KillDamage = 99999;
        // 숫자 줄과 겹치지 않는 F키만 쓴다
        private const float OverlayRefreshSeconds = 0.25f;
        private const float CountRefreshSeconds = 0.5f;

        [SerializeField] private Transform[] _teleportPoints;
        [SerializeField] private float _flySpeed = 25f;
        [SerializeField] private float[] _timeScales = { 1f, 2f, 4f, 0.25f };

        private readonly StringBuilder _text = new StringBuilder(256);
        private Transform _player;
        private Health _playerHealth;
        private CharacterController _controller;
        private FirstPersonMovement _movement;
        private Transform _camera;
        private GUIStyle _style;
        private string _overlay = string.Empty;
        private float _nextOverlayTime;
        private float _nextCountTime;
        private float _smoothedFrameTime = 0.016f;
        private int _aliveEnemies;
        private int _teleportIndex = -1;
        private int _timeScaleIndex;
        private bool _godMode;
        private bool _flying;
        private bool _showOverlay = true;

        private void Awake()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GameObject player = GameObject.FindWithTag(PlayerTag);
            if (player == null)
            {
                Debug.LogError("DebugCheats: Player 태그 오브젝트를 찾지 못했습니다", this);
                enabled = false;
                return;
            }

            _player = player.transform;
            _playerHealth = player.GetComponent<Health>();
            _controller = player.GetComponent<CharacterController>();
            _movement = player.GetComponent<FirstPersonMovement>();

            Camera main = Camera.main;
            _camera = main != null ? main.transform : null;
#else
            enabled = false;
#endif
        }

        private void OnDisable()
        {
            // 배속과 비행을 켠 채 꺼지면 이후 정상 플레이가 망가진다
            Time.timeScale = 1f;
            SetFlying(false);
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                _godMode = !_godMode;
            }

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                TeleportRelative(1);
            }

            if (keyboard.f3Key.wasPressedThisFrame)
            {
                TeleportRelative(-1);
            }

            if (keyboard.f4Key.wasPressedThisFrame)
            {
                KillAllEnemies();
            }

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                SetFlying(!_flying);
            }

            if (keyboard.f6Key.wasPressedThisFrame)
            {
                CycleTimeScale();
            }

            if (keyboard.f7Key.wasPressedThisFrame)
            {
                _showOverlay = !_showOverlay;
            }

            if (_godMode && _playerHealth != null && !_playerHealth.IsDead && _playerHealth.Current < _playerHealth.Max)
            {
                _playerHealth.Heal(_playerHealth.Max);
            }

            if (_flying)
            {
                Fly(keyboard);
            }

            _smoothedFrameTime = Mathf.Lerp(_smoothedFrameTime, Time.unscaledDeltaTime, 0.1f);
            RefreshOverlay();
#endif
        }

        private void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_showOverlay)
            {
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
                _style.normal.textColor = Color.white;
            }

            GUI.Label(new Rect(10f, 10f, 520f, 160f), _overlay, _style);
#endif
        }

        private void TeleportRelative(int step)
        {
            if (_teleportPoints == null || _teleportPoints.Length == 0)
            {
                return;
            }

            int count = _teleportPoints.Length;
            _teleportIndex = _teleportIndex < 0
                ? (step > 0 ? 0 : count - 1)
                : (_teleportIndex + step + count) % count;

            Teleport(_teleportPoints[_teleportIndex]);
        }

        private void Teleport(Transform point)
        {
            if (point == null)
            {
                return;
            }

            // 컨트롤러가 켜진 채로 위치를 바꾸면 다음 Move에서 이전 위치로 되돌려진다
            bool wasEnabled = _controller != null && _controller.enabled;
            if (_controller != null)
            {
                _controller.enabled = false;
            }

            _player.position = new Vector3(point.position.x, 0.1f, point.position.z);
            _player.rotation = Quaternion.Euler(0f, point.eulerAngles.y, 0f);

            if (_controller != null)
            {
                _controller.enabled = wasEnabled;
            }
        }

        private void KillAllEnemies()
        {
            Health[] all = FindObjectsByType<Health>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != _playerHealth && !all[i].IsDead)
                {
                    all[i].TakeDamage(KillDamage);
                }
            }
        }

        private void CycleTimeScale()
        {
            if (_timeScales == null || _timeScales.Length == 0)
            {
                return;
            }

            _timeScaleIndex = (_timeScaleIndex + 1) % _timeScales.Length;
            Time.timeScale = _timeScales[_timeScaleIndex];
        }

        // 벽과 소품을 통과해 맵 구석을 훑을 수 있게 컨트롤러와 일반 이동을 끈다
        private void SetFlying(bool value)
        {
            _flying = value;

            if (_controller != null)
            {
                _controller.enabled = !value;
            }

            if (_movement != null)
            {
                _movement.enabled = !value;
            }
        }

        private void Fly(Keyboard keyboard)
        {
            Transform reference = _camera != null ? _camera : _player;
            Vector3 direction = Vector3.zero;

            if (keyboard.wKey.isPressed)
            {
                direction += reference.forward;
            }

            if (keyboard.sKey.isPressed)
            {
                direction -= reference.forward;
            }

            if (keyboard.dKey.isPressed)
            {
                direction += reference.right;
            }

            if (keyboard.aKey.isPressed)
            {
                direction -= reference.right;
            }

            if (keyboard.spaceKey.isPressed)
            {
                direction += Vector3.up;
            }

            if (keyboard.leftCtrlKey.isPressed)
            {
                direction -= Vector3.up;
            }

            // 배속을 켜도 비행 속도는 일정해야 해서 실제 시간으로 움직인다
            _player.position += direction.normalized * (_flySpeed * Time.unscaledDeltaTime);
        }

        private void RefreshOverlay()
        {
            if (!_showOverlay || Time.unscaledTime < _nextOverlayTime)
            {
                return;
            }

            _nextOverlayTime = Time.unscaledTime + OverlayRefreshSeconds;

            if (Time.unscaledTime >= _nextCountTime)
            {
                _nextCountTime = Time.unscaledTime + CountRefreshSeconds;
                _aliveEnemies = CountAliveEnemies();
            }

            _text.Clear();
            _text.Append("FPS ").Append(Mathf.RoundToInt(1f / Mathf.Max(_smoothedFrameTime, 0.0001f)));
            _text.Append("   적 ").Append(_aliveEnemies);
            _text.Append("   배속 ").Append(Time.timeScale);
            _text.AppendLine();
            _text.Append("[F1] 무적 ").Append(_godMode ? "ON" : "off");
            _text.Append("  [F5] 비행 ").Append(_flying ? "ON" : "off");
            _text.AppendLine();
            _text.Append("[F2/F3] 이동 ");

            if (_teleportIndex >= 0 && _teleportPoints != null && _teleportIndex < _teleportPoints.Length && _teleportPoints[_teleportIndex] != null)
            {
                Transform point = _teleportPoints[_teleportIndex];
                _text.Append(_teleportIndex + 1).Append('/').Append(_teleportPoints.Length).Append(' ');
                _text.Append(point.parent != null ? point.parent.name + "/" : string.Empty).Append(point.name);
            }
            else
            {
                _text.Append("-");
            }

            _text.AppendLine();
            _text.Append("[F4] 전체 처치  [F6] 배속  [F7] 표시");
            _overlay = _text.ToString();
        }

        private int CountAliveEnemies()
        {
            Health[] all = FindObjectsByType<Health>(FindObjectsSortMode.None);
            int count = 0;

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != _playerHealth && !all[i].IsDead)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
