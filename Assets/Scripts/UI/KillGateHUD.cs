using System;
using System.Collections;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    // 해골 문까지 남은 처치 수를 보여 준다. 스테이지마다 문이 따로라서 가장 최근에 진행된 문을 따라간다
    public class KillGateHUD : MonoBehaviour
    {
        // 이벤트가 어느 문에서 왔는지 알려 주지 않아서 문마다 따로 핸들러를 걸어 둔다
        private class GateBinding
        {
            public KillGate Gate;
            public Action<int, int> OnProgress;
            public Action OnEngaged;
            public Action OnUnlocked;
        }

        [Tooltip("스테이지 순서대로. 첫 문은 시작부터 보이고, 이후 문은 그 스테이지의 첫 적이 나올 때 이어받는다")]
        [SerializeField] private KillGate[] _gates;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Text _label;
        [Tooltip("{0}은 현재 처치 수, {1}은 목표")]
        [SerializeField] private string _progressFormat = "{0} / {1}";
        [SerializeField] private string _unlockedText = "해골 문이 열렸다";
        [SerializeField] private float _unlockedSeconds = 3f;

        private GateBinding[] _bindings;
        private Coroutine _hideRoutine;

        private void OnEnable()
        {
            SetVisible(false);

            if (_gates == null || _gates.Length == 0)
            {
                return;
            }

            _bindings = new GateBinding[_gates.Length];

            for (int i = 0; i < _gates.Length; i++)
            {
                KillGate gate = _gates[i];
                if (gate == null)
                {
                    continue;
                }

                var binding = new GateBinding { Gate = gate };
                binding.OnProgress = (kills, required) => ShowProgress(kills, required);
                binding.OnEngaged = () => ShowProgress(gate.Kills, gate.RequiredKills);
                binding.OnUnlocked = ShowUnlocked;

                gate.ProgressChanged += binding.OnProgress;
                gate.Engaged += binding.OnEngaged;
                gate.Unlocked += binding.OnUnlocked;
                _bindings[i] = binding;
            }

            // 처치 전에도 목표가 보이도록 첫 문의 진행도를 바로 띄운다
            if (_gates[0] != null && !_gates[0].IsUnlocked)
            {
                ShowProgress(_gates[0].Kills, _gates[0].RequiredKills);
            }
        }

        private void OnDisable()
        {
            if (_bindings == null)
            {
                return;
            }

            for (int i = 0; i < _bindings.Length; i++)
            {
                GateBinding binding = _bindings[i];
                if (binding == null || binding.Gate == null)
                {
                    continue;
                }

                binding.Gate.ProgressChanged -= binding.OnProgress;
                binding.Gate.Engaged -= binding.OnEngaged;
                binding.Gate.Unlocked -= binding.OnUnlocked;
            }

            _bindings = null;
        }

        private void ShowProgress(int kills, int required)
        {
            if (kills >= required)
            {
                return;
            }

            StopHide();
            _label.text = string.Format(_progressFormat, kills, required);
            SetVisible(true);
        }

        private void ShowUnlocked()
        {
            StopHide();
            _label.text = _unlockedText;
            SetVisible(true);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(_unlockedSeconds);
            SetVisible(false);
            _hideRoutine = null;
        }

        private void StopHide()
        {
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }
        }

        private void SetVisible(bool visible)
        {
            if (_group != null)
            {
                _group.alpha = visible ? 1f : 0f;
            }
        }
    }
}
