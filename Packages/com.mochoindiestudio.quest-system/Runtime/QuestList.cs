using System.Collections.Generic;
using UnityEngine;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// An ordered collection of <see cref="Quest"/> assets forming a tutorial, a campaign, a
    /// side-quest set, and so on. A quest may appear in more than one list. Registering a list with a
    /// <see cref="QuestLog"/> registers all its quests.
    ///
    /// The list also stores the canvas layout for the Quest List graph window (one position per
    /// quest, kept aligned with <see cref="Quests"/> by that editor) so a quest that appears in
    /// several lists can sit in a different place in each.
    /// </summary>
    [CreateAssetMenu(fileName = "New Quest List", menuName = "MIS Quest System/Quest List")]
    public sealed class QuestList : ScriptableObject
    {
        [SerializeField]
        private List<Quest> quests = new List<Quest>();

        [Tooltip("Convenience for linear lists (e.g. a tutorial): start the first quest when the list " +
                 "is registered, and start the next quest in order whenever one completes. Leave off " +
                 "and use per-quest prerequisites for anything non-linear.")]
        [SerializeField]
        private bool autoAdvance;

        [HideInInspector]
        [SerializeField]
        private List<Vector2> nodePositions = new List<Vector2>();

        /// <summary>The quests in this list, in order. Never null; entries may be null if unassigned in the editor.</summary>
        public List<Quest> Quests => quests;

        /// <summary>See the field tooltip.</summary>
        public bool AutoAdvance => autoAdvance;

        /// <summary>Graph-window canvas position for the quest at <paramref name="index"/>; zero when
        /// none has been stored yet.</summary>
        public Vector2 GetNodePosition(int index)
        {
            return index >= 0 && index < nodePositions.Count ? nodePositions[index] : Vector2.zero;
        }

        /// <summary>Stores the graph-window canvas position for the quest at <paramref name="index"/>,
        /// growing the backing list to match <see cref="Quests"/> as needed. Editor-only concern.</summary>
        public void SetNodePosition(int index, Vector2 position)
        {
            if (index < 0)
            {
                return;
            }

            while (nodePositions.Count <= index)
            {
                nodePositions.Add(Vector2.zero);
            }

            nodePositions[index] = position;
        }

        private void OnValidate()
        {
            // Keep the layout list from outgrowing the quest list after manual edits.
            while (nodePositions.Count > quests.Count)
            {
                nodePositions.RemoveAt(nodePositions.Count - 1);
            }
        }
    }
}
