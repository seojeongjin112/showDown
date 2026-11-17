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

        // 피치는 건드리지 않고 yaw만 맞춘다. 스프라이트가 지면에 항상 수직으로 서 있어야 2.5D로 읽힌다
        private void LateUpdate()
        {
            transform.rotation = Quaternion.Euler(0f, _camera.eulerAngles.y, 0f);
        }
    }
}
