using UnityEngine;

namespace ArenaBreak.Core
{
    public class Billboard : MonoBehaviour
    {
        private Transform _camera;

        private void Awake()
        {
            Camera main = Camera.main;
            if (main == null)
            {
                enabled = false;
                return;
            }

            _camera = main.transform;
        }

        // yaw만 맞추면 원근 카메라에서 건물이 바깥으로 기울어 보인다. 카메라 회전 전체를 따라야 항상 정면으로 보인다
        private void LateUpdate()
        {
            transform.rotation = _camera.rotation;
        }
    }
}
