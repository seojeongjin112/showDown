using UnityEngine;

namespace ArenaBreak.Core
{
    // 선인장·나무처럼 땅에 서 있는 그림 판이 카메라 방향으로 눕거나 기울지 않도록 좌우 회전만 따라간다
    public class YawBillboard : MonoBehaviour
    {
        private Transform _camera;

        private void Awake()
        {
            Camera main = Camera.main;
            if (main == null)
            {
                Debug.LogError("YawBillboard: 메인 카메라를 찾을 수 없습니다", this);
                enabled = false;
                return;
            }

            _camera = main.transform;
        }

        // 카메라가 움직인 뒤의 방향을 따라가야 같은 프레임에 어긋나지 않는다
        private void LateUpdate()
        {
            transform.rotation = Quaternion.Euler(0f, _camera.eulerAngles.y, 0f);
        }
    }
}
