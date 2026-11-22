using System.Collections;
using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class VictoryScreen : MonoBehaviour
    {
        [SerializeField] private WaveSpawner _spawner;
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Text _message;
        [Tooltip("{0}은 전체 웨이브 수")]
        [SerializeField] private string _messageFormat = "{0}웨이브를 모두 버텨냈다!";
        // 마지막 적을 잡은 뒤 바로 멈추면 떨어진 코인과 구슬을 볼 틈이 없어서 잠시 기다린다
        [SerializeField] private float _showDelay = 1.5f;
        // 시점·이동·공격 입력을 꺼야 한다. 시점 스크립트는 커서가 풀려 있으면 클릭으로 다시 잠가 버려서 버튼을 누를 수 없다
        [SerializeField] private Behaviour[] _disableOnVictory;
        // 커서가 풀린 동안 화면 중앙에 조준점이 남으면 마우스 포인터와 겹쳐 보인다
        [SerializeField] private GameObject[] _hideOnVictory;

        private void Awake()
        {
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            _spawner.AllWavesCleared += OnAllWavesCleared;
            _retryButton.onClick.AddListener(Retry);
        }

        private void OnDisable()
        {
            _spawner.AllWavesCleared -= OnAllWavesCleared;
            _retryButton.onClick.RemoveListener(Retry);
        }

        private void OnAllWavesCleared()
        {
            StartCoroutine(ShowAfterDelay());
        }

        // 게임 시간으로 기다려야 그 사이에 플레이어가 죽거나 레벨업 창이 열려 시간이 멈추면 승리 화면도 함께 미뤄진다
        private IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSeconds(_showDelay);
            Show();
        }

        private void Show()
        {
            foreach (Behaviour behaviour in _disableOnVictory)
            {
                behaviour.enabled = false;
            }

            foreach (GameObject target in _hideOnVictory)
            {
                target.SetActive(false);
            }

            _message.text = string.Format(_messageFormat, _spawner.TotalWaves);
            _root.SetActive(true);
            Time.timeScale = 0f;
        }

        private void Retry()
        {
            SceneReloader.Reload(this);
        }
    }
}
