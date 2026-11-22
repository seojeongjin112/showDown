using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaBreak.UI
{
    // 게임오버와 승리 화면이 같은 방식으로 현재 씬을 다시 시작하도록 한곳에 둔다
    public static class SceneReloader
    {
        public static void Reload(Object context)
        {
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            if (buildIndex < 0)
            {
                Debug.LogError("SceneReloader: 현재 씬이 Build Profiles의 Scene List에 없어서 다시 시작할 수 없습니다", context);
                return;
            }

            // 멈춘 시간을 되돌리지 않고 로드하면 다음 판도 멈춘 채 시작한다
            Time.timeScale = 1f;
            SceneManager.LoadScene(buildIndex);
        }
    }
}
