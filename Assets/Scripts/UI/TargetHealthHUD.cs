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

        // 한 레이에 겹치는 콜라이더가 이만큼이면 앞에서부터 충분하다
        private const int MaxRayHits = 8;

        private readonly RaycastHit[] _hits = new RaycastHit[MaxRayHits];
        private Transform _selfRoot;
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

            _selfRoot = _camera.transform.root;
            _root.SetActive(false);
        }

        private void OnDisable()
        {
            SetTarget(null);
        }

        private void Update()
        {
            Collider hitCollider = FindNearestOtherCollider();

            // 매 프레임 GetComponent를 피하려고 바라보는 콜라이더가 바뀔 때만 Health를 찾는다
            if (hitCollider != _lastCollider)
            {
                _lastCollider = hitCollider;
                SetTarget(hitCollider != null ? hitCollider.GetComponentInParent<Health>() : null);
            }
        }

        // 시점이 아래를 향하면 레이가 플레이어 자신의 콜라이더에 맞아 내 체력이 적 체력처럼 뜨므로 자기 몸은 건너뛴다
        private Collider FindNearestOtherCollider()
        {
            Transform view = _camera.transform;
            int count = Physics.RaycastNonAlloc(view.position, view.forward, _hits, _range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

            Collider nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (hit.distance < nearestDistance && !hit.collider.transform.IsChildOf(_selfRoot))
                {
                    nearest = hit.collider;
                    nearestDistance = hit.distance;
                }
            }

            return nearest;
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
