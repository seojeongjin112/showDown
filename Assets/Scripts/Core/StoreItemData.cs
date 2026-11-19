using UnityEngine;

namespace ArenaBreak.Core
{
    public enum StoreItemCategory
    {
        Upgrade,
        Weapon
    }

    [CreateAssetMenu(fileName = "StoreItem", menuName = "ArenaBreak/Store Item")]
    public class StoreItemData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private StoreItemCategory _category;
        [SerializeField] private int _price;
        [SerializeField, TextArea] private string _description;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public StoreItemCategory Category => _category;
        public int Price => _price;
        public string Description => _description;
    }
}
