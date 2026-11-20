using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class TargetHealthHUD : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _range = 30f;
        [SerializeField] private GameObject _root;
        [Tooltip("왼쪽부터 채워진다. 적마다 최대 체력이 달라서 칸 수 대신 비율로 채운다")]
        [SerializeField] private Image[] _cells;

        private Collider _lastCollider;
        private Health _target;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                Debug.LogError("TargetHealthHUD: 카메라를 찾을 수 없습니다", this);
                enabled = false;
                return;
            }

            _root.SetActive(false);
        }

        private void OnDisable()
        {
            SetTarget(null);
        }

        private void Update()
        {
            Transform view = _camera.transform;
            // 적 콜라이더는 트리거라서 KnifeDamage와 같이 트리거도 맞도록 한다
            Collider hitCollider = Physics.Raycast(view.position, view.forward, out RaycastHit hit, _range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)
                ? hit.collider
                : null;

            // 매 프레임 GetComponent를 피하려고 바라보는 콜라이더가 바뀔 때만 Health를 찾는다
            if (hitCollider != _lastCollider)
            {
                _lastCollider = hitCollider;
                SetTarget(hitCollider != null ? hitCollider.GetComponentInParent<Health>() : null);
            }
        }

        private void SetTarget(Health target)
        {
            if (_target != null)
            {
                _target.Changed -= Refresh;
            }

            _target = target;

            if (_target == null || _target.IsDead)
            {
                _target = null;
                _root.SetActive(false);
                return;
            }

            _target.Changed += Refresh;
            _root.SetActive(true);
            Refresh(_target.Current, _target.Max);
        }

        private void Refresh(int current, int max)
        {
            if (current <= 0)
            {
                SetTarget(null);
                return;
            }

            // 1/10만 남아도 한 칸은 보이도록 올림한다
            int filled = Mathf.CeilToInt((float)current / max * _cells.Length);

            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].enabled = i < filled;
            }
        }
    }
}
