using UnityEngine;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// A demo inventory item: a stable id (used as the quest-signal payload), a display name and an
    /// icon sprite for the inventory UI.
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "MIS Quest System Demo/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private Sprite icon;

        /// <summary>Stable identifier; also the payload sent with <see cref="DemoSignals.ItemCollected"/>.</summary>
        public string Id => id;

        /// <summary>Player-facing name for the inventory list.</summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>Icon for the inventory grid; may be null.</summary>
        public Sprite Icon => icon;
    }
}
