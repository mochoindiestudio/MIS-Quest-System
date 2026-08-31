using System;
using UnityEngine;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Passes when another quest is in an expected <see cref="QuestState"/>. This is how
    /// "finishing quest A unlocks quest B" is expressed -- add it to quest B's
    /// <see cref="Quest.Prerequisites"/> with <see cref="expectedState"/> = <see cref="QuestState.Completed"/>.
    /// </summary>
    [Serializable]
    public sealed class QuestStateCondition : QuestCondition
    {
        [Tooltip("Quest.Id of the quest to test. Use the read-only Id field shown on the Quest asset.")]
        [SerializeField]
        private string questId;

        [SerializeField]
        private QuestState expectedState = QuestState.Completed;

        /// <summary>The <see cref="Quest.Id"/> whose state is tested.</summary>
        public string QuestId
        {
            get => questId;
            set => questId = value;
        }

        /// <summary>The state <see cref="QuestId"/> must be in for this condition to pass.</summary>
        public QuestState ExpectedState
        {
            get => expectedState;
            set => expectedState = value;
        }

        /// <inheritdoc />
        public override bool Evaluate(IQuestContext context)
        {
            if (context == null || string.IsNullOrEmpty(questId))
            {
                return false;
            }

            return context.GetQuestState(questId) == expectedState;
        }
    }
}
