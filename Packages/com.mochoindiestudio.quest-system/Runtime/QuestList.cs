using System.Collections.Generic;
using UnityEngine;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// An ordered collection of <see cref="Quest"/> assets forming a tutorial, a campaign, a
    /// side-quest set, and so on. A quest may appear in more than one list. Registering a list with a
    /// <see cref="QuestLog"/> registers all its quests.
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

        /// <summary>The quests in this list, in order. Never null; entries may be null if unassigned in the editor.</summary>
        public List<Quest> Quests => quests;

        /// <summary>See the field tooltip.</summary>
        public bool AutoAdvance => autoAdvance;
    }
}
