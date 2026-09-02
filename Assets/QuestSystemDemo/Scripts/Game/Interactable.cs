using UnityEngine;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// Something the player can press E on when in range. The <see cref="PlayerController"/> tracks
    /// the nearest in-range interactable, shows its <see cref="Prompt"/>, and calls
    /// <see cref="Interact"/> on the interact key.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField]
        private Inventory inventory;

        /// <summary>The player's inventory, wired by the scene builder. May be used by subclasses.</summary>
        protected Inventory Inventory => inventory;

        /// <summary>Text shown near the player while this is the active interactable, e.g. "Pick up bucket".</summary>
        public abstract string Prompt { get; }

        /// <summary>Whether pressing E does anything right now. A false result still shows the prompt.</summary>
        public virtual bool CanInteract => true;

        /// <summary>Runs the interaction.</summary>
        public abstract void Interact();
    }
}
