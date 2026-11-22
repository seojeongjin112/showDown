using System.Collections;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class LevelUpBannerHUD : MonoBehaviour
    {
        [SerializeField] private Experience _experience;
        [Tooltip("이 컴포넌트가 붙은 오브젝트와 다른 오브젝트여야 숨겨도 코루틴이 멈추지 않는다")]
        [SerializeField] private GameObject _banner;
        [SerializeField] private Text _label;
        [SerializeField] private string _labelFormat = "레벨 {0}!";
        [SerializeField] private float _showDuration = 2f;

        private Coroutine _showRoutine;

        private void OnEnable()
        {
            if (_experience == null)
            {
                return;
            }

            _banner.SetActive(false);
            _experience.LeveledUp += OnLeveledUp;
        }

        private void OnDisable()
        {
            if (_experience != null)
            {
                _experience.LeveledUp -= OnLeveledUp;
            }
        }

        private void OnLeveledUp(int level)
        {
            _label.text = string.Format(_labelFormat, level);

            // 연속으로 레벨이 오르면 표시 시간을 처음부터 다시 센다
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
            }

            _showRoutine = StartCoroutine(Show());
        }

        private IEnumerator Show()
        {
            _banner.SetActive(true);
            // 레벨업 직후 강화 창이 시간을 멈추므로, 게임 시간으로 세면 창을 닫을 때까지 배너가 남는다
            yield return new WaitForSecondsRealtime(_showDuration);
            _banner.SetActive(false);
            _showRoutine = null;
        }
    }
}
