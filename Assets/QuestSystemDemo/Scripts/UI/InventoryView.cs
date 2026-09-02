using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// The inventory panel (I). Shows the bag as an icon grid and offers the demo's single crafting
    /// action -- a "Craft bucket on a rope" button that is only interactable while a bucket and a
    /// rope are held.
    /// </summary>
    public sealed class InventoryView : MonoBehaviour
    {
        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private RectTransform slotContainer;

        [SerializeField]
        private InventorySlotView slotTemplate;

        [SerializeField]
        private Button craftButton;

        [SerializeField]
        private GameObject emptyLabel;

        [SerializeField]
        private Inventory inventory;

        [SerializeField]
        private DemoGame game;

        private readonly List<InventorySlotView> slots = new List<InventorySlotView>();

        /// <summary>Whether the panel is currently shown.</summary>
        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (slotTemplate != null)
            {
                slotTemplate.gameObject.SetActive(false);
            }

            SetOpen(false);
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.OnChanged += Refresh;
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.OnChanged -= Refresh;
            }
        }

        private void Start()
        {
            if (craftButton != null)
            {
                craftButton.onClick.AddListener(OnCraftClicked);
            }

            Refresh();
        }

        /// <summary>Flips the panel open/closed.</summary>
        public void Toggle() => SetOpen(!IsOpen);

        /// <summary>Shows or hides the panel.</summary>
        public void SetOpen(bool open)
        {
            if (panel != null)
            {
                panel.SetActive(open);
            }

            if (open)
            {
                Refresh();
            }
        }

        private void OnCraftClicked()
        {
            if (game != null)
            {
                game.TryCraftBucketOnRope();
            }

            Refresh();
        }

        /// <summary>Rebuilds the slot grid and updates the craft button.</summary>
        public void Refresh()
        {
            if (inventory == null || slotTemplate == null)
            {
                return;
            }

            IReadOnlyList<ItemDefinition> items = inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                InventorySlotView slot = i < slots.Count ? slots[i] : CreateSlot();
                slot.gameObject.SetActive(true);
                slot.Bind(items[i]);
            }

            for (int i = items.Count; i < slots.Count; i++)
            {
                slots[i].gameObject.SetActive(false);
            }

            if (emptyLabel != null)
            {
                emptyLabel.SetActive(items.Count == 0);
            }

            if (craftButton != null)
            {
                craftButton.interactable =
                    inventory.Has(DemoSignals.Bucket) &&
                    inventory.Has(DemoSignals.Rope) &&
                    !inventory.Has(DemoSignals.BucketOnRope);
            }
        }

        private InventorySlotView CreateSlot()
        {
            InventorySlotView slot = Instantiate(slotTemplate, slotContainer);
            slots.Add(slot);
            return slot;
        }
    }
}
