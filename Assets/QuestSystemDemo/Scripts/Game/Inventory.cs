using System;
using System.Collections.Generic;
using UnityEngine;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// The player's bag of <see cref="ItemDefinition"/>s. Deliberately knows nothing about quests --
    /// pickups and the crafting rule raise quest signals themselves.
    /// </summary>
    public sealed class Inventory : MonoBehaviour
    {
        private readonly List<ItemDefinition> items = new List<ItemDefinition>();

        /// <summary>Raised after any add or remove, for the inventory UI to redraw.</summary>
        public event Action OnChanged;

        /// <summary>The current contents, in pickup order.</summary>
        public IReadOnlyList<ItemDefinition> Items => items;

        /// <summary>Adds an item (duplicates allowed) and notifies listeners.</summary>
        public void Add(ItemDefinition item)
        {
            if (item == null)
            {
                return;
            }

            items.Add(item);
            OnChanged?.Invoke();
        }

        /// <summary>Removes the first item with the given id. Returns true if one was removed.</summary>
        public bool Remove(string id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].Id == id)
                {
                    items.RemoveAt(i);
                    OnChanged?.Invoke();
                    return true;
                }
            }

            return false;
        }

        /// <summary>Whether the bag holds at least one item with the given id.</summary>
        public bool Has(string id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].Id == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
