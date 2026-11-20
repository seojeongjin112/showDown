using ArenaBreak.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBreak.UI
{
    public class CoinCounterHUD : MonoBehaviour
    {
        [SerializeField] private CoinWallet _wallet;
        [SerializeField] private Text _label;

        private void OnEnable()
        {
            if (_wallet == null)
            {
                return;
            }

            _wallet.Changed += Refresh;
            Refresh(_wallet.Count);
        }

        private void OnDisable()
        {
            if (_wallet != null)
            {
                _wallet.Changed -= Refresh;
            }
        }

        private void Refresh(int count)
        {
            _label.text = count.ToString();
        }
    }
}
