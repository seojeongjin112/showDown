using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    // 튜토리얼 안내판. 제목과 설명, 조작 아이콘(움직이는 키·마우스 그림), 진행도를 보여 주고 완료하면 잠깐 완료 문구를 띄운다
    public class TutorialPromptHUD : MonoBehaviour
    {
        private const int MaxIcons = 6;

        [SerializeField] private TutorialManager _manager;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Text _title;
        [SerializeField] private Text _message;
        [SerializeField] private Text _progressLabel;
        [Tooltip("아이콘 Image를 이 아래에 만든다. 가로로 나란히 놓인다")]
        [SerializeField] private RectTransform _iconRow;
        [Tooltip("도트 그림을 이 배율로 키워서 보여 준다")]
        [SerializeField] private float _iconScale = 3f;
        [SerializeField] private string _completeText = "완료!";
        [SerializeField] private string _skipHint = "[Tab] 건너뛰기";

        private readonly Image[] _icons = new Image[MaxIcons];
        private readonly Text[] _iconLabels = new Text[MaxIcons];
        private TutorialManager.IconSet[] _iconSets;
        private int _target;
        private bool _completed;

        private void Awake()
        {
            if (_manager == null || _group == null || _iconRow == null)
            {
                Debug.LogError("TutorialPromptHUD: 매니저, 그룹 또는 아이콘 줄 참조가 비어 있습니다", this);
                enabled = false;
                return;
            }

            // 아이콘은 단계가 바뀔 때마다 만들지 않고 미리 만들어 두고 켜고 끈다
            for (int i = 0; i < MaxIcons; i++)
            {
                GameObject slot = new GameObject("Icon" + i, typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(_iconRow, false);
                _icons[i] = slot.GetComponent<Image>();
                _icons[i].raycastTarget = false;
                slot.SetActive(false);

                // 아이콘 옆 글자는 첫 아이콘 그림의 자식 텍스트로 둔다
                GameObject label = new GameObject("Label", typeof(RectTransform), typeof(Text));
                label.transform.SetParent(slot.transform, false);
                RectTransform rect = label.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -4f);
                rect.sizeDelta = new Vector2(160f, 30f);
                Text text = label.GetComponent<Text>();
                text.font = _message.font;
                text.fontSize = 18;
                text.alignment = TextAnchor.UpperCenter;
                text.color = _message.color;
                text.raycastTarget = false;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                _iconLabels[i] = text;
            }
        }

        private void OnEnable()
        {
            _group.alpha = 0f;
            _manager.StepStarted += OnStepStarted;
            _manager.ProgressChanged += OnProgressChanged;
            _manager.StepCompleted += OnStepCompleted;
            _manager.Finished += OnFinished;
        }

        private void OnDisable()
        {
            _manager.StepStarted -= OnStepStarted;
            _manager.ProgressChanged -= OnProgressChanged;
            _manager.StepCompleted -= OnStepCompleted;
            _manager.Finished -= OnFinished;
        }

        // 아이콘 애니메이션은 시간으로 프레임을 고르므로 매 프레임 그림만 바꾼다
        private void Update()
        {
            if (_iconSets == null)
            {
                return;
            }

            for (int i = 0; i < _iconSets.Length && i < MaxIcons; i++)
            {
                Sprite[] frames = _iconSets[i].Frames;
                if (frames == null || frames.Length == 0)
                {
                    continue;
                }

                int frame = (int)(Time.unscaledTime * _iconSets[i].FramesPerSecond) % frames.Length;
                _icons[i].sprite = frames[frame];
            }
        }

        private void OnStepStarted(int index, TutorialManager.Step step)
        {
            _completed = false;
            _group.alpha = 1f;
            // 표적이 여럿이라 몇 번째인지 제목에 함께 알려 준다
            _title.text = step.MarkNumber > 0 ? step.Title + "  (" + step.MarkNumber + "번 표적)" : step.Title;
            _message.text = step.Message + "\n" + _skipHint;
            _target = Mathf.Max(1, step.Goal == TutorialGoal.MoveAndLook ? 100 : step.Target);
            _iconSets = step.Icons;

            for (int i = 0; i < MaxIcons; i++)
            {
                bool used = _iconSets != null && i < _iconSets.Length && _iconSets[i].Frames != null && _iconSets[i].Frames.Length > 0;
                _icons[i].gameObject.SetActive(used);

                if (!used)
                {
                    continue;
                }

                _icons[i].sprite = _iconSets[i].Frames[0];
                Rect rect = _iconSets[i].Frames[0].rect;
                _icons[i].rectTransform.sizeDelta = new Vector2(rect.width * _iconScale, rect.height * _iconScale);
                _iconLabels[i].text = _iconSets[i].Label;
            }
        }

        private void OnProgressChanged(int current, int target)
        {
            if (_completed)
            {
                return;
            }

            // 이동·시선 단계는 100을 기준으로 한 비율이라 숫자 대신 퍼센트로 보여 준다
            _progressLabel.text = target == 100 ? current + "%" : current + " / " + target;
        }

        private void OnStepCompleted()
        {
            _completed = true;
            _progressLabel.text = _completeText;
        }

        private void OnFinished()
        {
            _group.alpha = 0f;
        }
    }
}
