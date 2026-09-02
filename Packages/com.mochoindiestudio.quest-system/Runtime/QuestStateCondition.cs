using System;
using UnityEngine;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Passes when another quest is in an expected <see cref="QuestState"/>. Drag the quest asset in
    /// -- no ids to copy. Use it inside a <see cref="CompositeCondition"/> on a
    /// <see cref="Quest.AdvancedUnlock"/> for prerequisites the <see cref="Quest.UnlockedBy"/> edge
    /// graph can't express (e.g. "unlocked once quest A is <see cref="QuestState.Failed"/>"), or as an
    /// <see cref="Objective.CompleteWhen"/> that waits on another quest.
    /// </summary>
    [Serializable]
    public sealed class QuestStateCondition : QuestCondition
    {
        [Tooltip("The quest whose state is tested.")]
        [SerializeField]
        private Quest quest;

        [SerializeField]
        private QuestState expectedState = QuestState.Completed;

        /// <summary>The quest whose state is tested.</summary>
        public Quest Quest
        {
            get => quest;
            set => quest = value;
        }

        /// <summary>The state <see cref="Quest"/> must be in for this condition to pass.</summary>
        public QuestState ExpectedState
        {
            get => expectedState;
            set => expectedState = value;
        }

        /// <inheritdoc />
        public override bool Evaluate(in QuestConditionContext context)
        {
            if (context.Quest == null || quest == null || string.IsNullOrEmpty(quest.Id))
            {
                return false;
            }

            return context.Quest.GetQuestState(quest.Id) == expectedState;
        }
    }
}
