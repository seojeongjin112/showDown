using System;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    // HUD가 OnEnable에서 소유 상태와 탄약을 읽으므로 인벤토리 초기화가 먼저 끝나 있어야 한다
    [DefaultExecutionOrder(-100)]
    public class WeaponInventory : MonoBehaviour
    {
        [Serializable]
        private class Slot
        {
            [SerializeField] private WeaponData _data;
            [Tooltip("이 무기를 들었을 때 보이는 1인칭 팔 오브젝트")]
            [SerializeField] private GameObject _viewmodel;
            [SerializeField] private bool _ownedAtStart;

            public WeaponData Data => _data;
            public GameObject Viewmodel => _viewmodel;
            public bool OwnedAtStart => _ownedAtStart;
        }

        // 입력 에셋에 무기 전환 액션이 없어서 숫자키와 휠을 직접 바인딩한다
        private const string ScrollBinding = "<Mouse>/scroll";
        // 숫자키의 컨트롤 이름은 digit1이 아니라 1이라서 슬롯 번호를 그대로 쓴다
        private const string KeyBindingFormat = "<Keyboard>/{0}";

        [SerializeField] private Slot[] _slots;

        private bool[] _owned;
        private WeaponAmmo[] _ammo;
        private InputAction[] _selectActions;
        private InputAction _scrollAction;

        public int Count => _slots.Length;
        public int SelectedIndex { get; private set; }

        // HUD가 폴링하지 않고 구독해서 갱신하도록 바뀐 슬롯 번호를 넘긴다
        public event Action<int> SelectionChanged;
        public event Action<int> OwnedChanged;

        public WeaponData WeaponAt(int index) => _slots[index].Data;
        public bool IsOwned(int index) => _owned[index];
        public WeaponAmmo AmmoAt(int index) => _ammo[index];

        public int IndexOf(WeaponData weapon)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Data == weapon)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool CanUnlock(WeaponData weapon)
        {
            int index = IndexOf(weapon);
            return index >= 0 && !_owned[index];
        }

        public bool CanAddAmmo(WeaponData weapon)
        {
            int index = IndexOf(weapon);
            return index >= 0 && _owned[index] && _ammo[index] != null && _ammo[index].CanAddReserve;
        }

        public bool Unlock(WeaponData weapon)
        {
            if (!CanUnlock(weapon))
            {
                return false;
            }

            int index = IndexOf(weapon);
            _owned[index] = true;
            OwnedChanged?.Invoke(index);

            // 산 무기를 바로 들어야 샀다는 것이 눈에 보인다
            Select(index);
            return true;
        }

        public void AddAmmo(WeaponData weapon, int amount)
        {
            if (CanAddAmmo(weapon))
            {
                _ammo[IndexOf(weapon)].AddReserve(amount);
            }
        }

        public void Select(int index)
        {
            if (index < 0 || index >= _slots.Length || !_owned[index] || index == SelectedIndex)
            {
                return;
            }

            _slots[SelectedIndex].Viewmodel.SetActive(false);
            SelectedIndex = index;
            _slots[SelectedIndex].Viewmodel.SetActive(true);
            SelectionChanged?.Invoke(index);
        }

        private void Awake()
        {
            _owned = new bool[_slots.Length];
            _ammo = new WeaponAmmo[_slots.Length];
            _selectActions = new InputAction[_slots.Length];

            for (int i = 0; i < _slots.Length; i++)
            {
                _owned[i] = _slots[i].OwnedAtStart;

                if (_slots[i].Data.UsesAmmo)
                {
                    _ammo[i] = new WeaponAmmo(_slots[i].Data);

                    // 팔 오브젝트가 꺼져 있어도 탄약 상태를 넘길 수 있다
                    if (_slots[i].Viewmodel.TryGetComponent(out IAmmoWeapon weapon))
                    {
                        weapon.Bind(_ammo[i]);
                    }
                }

                _selectActions[i] = new InputAction("Weapon" + i, InputActionType.Button, string.Format(KeyBindingFormat, i + 1));
                _slots[i].Viewmodel.SetActive(i == SelectedIndex);
            }

            _scrollAction = new InputAction("WeaponScroll", InputActionType.Value, ScrollBinding);
        }

        private void OnEnable()
        {
            foreach (InputAction action in _selectActions)
            {
                action.Enable();
            }

            _scrollAction.Enable();
        }

        private void OnDisable()
        {
            if (_selectActions == null)
            {
                return;
            }

            foreach (InputAction action in _selectActions)
            {
                action.Disable();
            }

            _scrollAction.Disable();
        }

        private void OnDestroy()
        {
            if (_selectActions == null)
            {
                return;
            }

            foreach (InputAction action in _selectActions)
            {
                action.Dispose();
            }

            _scrollAction.Dispose();
        }

        private void Update()
        {
            for (int i = 0; i < _selectActions.Length; i++)
            {
                if (_selectActions[i].WasPressedThisFrame())
                {
                    Select(i);
                    return;
                }
            }

            // 휠 값의 크기는 장치마다 달라서 방향만 쓴다
            float scroll = _scrollAction.ReadValue<Vector2>().y;
            if (scroll > 0f)
            {
                Cycle(-1);
            }
            else if (scroll < 0f)
            {
                Cycle(1);
            }
        }

        // 가지지 않은 슬롯은 건너뛰고 끝에서 처음으로 이어진다
        private void Cycle(int direction)
        {
            for (int step = 1; step < _slots.Length; step++)
            {
                int index = (SelectedIndex + direction * step + _slots.Length * step) % _slots.Length;

                if (_owned[index])
                {
                    Select(index);
                    return;
                }
            }
        }
    }
}
