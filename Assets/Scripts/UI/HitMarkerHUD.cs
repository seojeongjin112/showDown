using System.Collections;
using ArenaBreak.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class HitMarkerHUD : MonoBehaviour
    {
        [SerializeField] private KnifeDamage _knife;
        [SerializeField] private GunWeapon[] _guns;
        [SerializeField] private Image _marker;
        [SerializeField] private Sprite _hitSprite;
        [SerializeField] private Sprite _killSprite;
        [SerializeField] private float _showSeconds = 0.15f;

        private WaitForSeconds _wait;
        private Coroutine _hiding;

        private void Awake()
        {
            _wait = new WaitForSeconds(_showSeconds);
            _marker.enabled = false;
        }

        private void OnEnable()
        {
            if (_knife != null)
            {
                _knife.Hit += OnHit;
            }

            foreach (GunWeapon gun in _guns)
            {
                gun.Hit += OnHit;
            }
        }

        private void OnDisable()
        {
            if (_knife != null)
            {
                _knife.Hit -= OnHit;
            }

            foreach (GunWeapon gun in _guns)
            {
                gun.Hit -= OnHit;
            }
        }

        private void OnHit(bool killed)
        {
            _marker.sprite = killed ? _killSprite : _hitSprite;
            _marker.enabled = true;

            // 연속 명중 때 앞선 타이머가 새 표시를 일찍 끄지 않도록 다시 시작한다
            if (_hiding != null)
            {
                StopCoroutine(_hiding);
            }

            _hiding = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return _wait;
            _marker.enabled = false;
            _hiding = null;
        }
    }
}
