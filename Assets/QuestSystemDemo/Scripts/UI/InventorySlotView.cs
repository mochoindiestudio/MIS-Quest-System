using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>One cell in the inventory grid: an icon and the item's name.</summary>
    public sealed class InventorySlotView : MonoBehaviour
    {
        [SerializeField]
        private Image icon;

        [SerializeField]
        private TMP_Text nameLabel;

        /// <summary>Fills the slot from <paramref name="item"/>.</summary>
        public void Bind(ItemDefinition item)
        {
            if (icon != null)
            {
                icon.sprite = item != null ? item.Icon : null;
                icon.enabled = icon.sprite != null;
            }

            if (nameLabel != null)
            {
                nameLabel.text = item != null ? item.DisplayName : string.Empty;
            }
        }
    }
}
