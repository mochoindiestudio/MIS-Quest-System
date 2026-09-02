using MochoIndieStudio.QuestSystem;
using UnityEngine;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// A world item. Press E in range to add it to the inventory and report
    /// <see cref="DemoSignals.ItemCollected"/> with the item's id, then it despawns.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ItemPickup : Interactable
    {
        [SerializeField]
        private ItemDefinition item;

        public override string Prompt => item != null ? $"Pick up {item.DisplayName}" : "Pick up";

        public override void Interact()
        {
            if (item == null || Inventory == null)
            {
                return;
            }

            Inventory.Add(item);
            QuestSignals.Report(DemoSignals.ItemCollected, item.Id);
            gameObject.SetActive(false);
        }
    }
}
