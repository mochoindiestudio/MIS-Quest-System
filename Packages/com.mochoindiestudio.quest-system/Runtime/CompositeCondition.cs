using System;
using System.Collections.Generic;
using UnityEngine;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Combines child conditions with a boolean <see cref="CompositeMode"/> (<c>All</c> / <c>Any</c> /
    /// <c>None</c>). Nest these to build arbitrary AND/OR/NOT trees.
    /// </summary>
    [Serializable]
    public sealed class CompositeCondition : QuestCondition
    {
        [SerializeField]
        private CompositeMode mode = CompositeMode.All;

        [SerializeReference]
        private List<QuestCondition> conditions = new List<QuestCondition>();

        /// <summary>How <see cref="Conditions"/> are combined.</summary>
        public CompositeMode Mode
        {
            get => mode;
            set => mode = value;
        }

        /// <summary>The child conditions. Never null; may be empty.</summary>
        public List<QuestCondition> Conditions => conditions;

        /// <inheritdoc />
        public override bool Evaluate(IQuestContext context)
        {
            bool anyTrue = false;
            bool allTrue = true;

            for (int i = 0; i < conditions.Count; i++)
            {
                QuestCondition child = conditions[i];
                bool result = child != null && child.Evaluate(context);

                anyTrue |= result;
                allTrue &= result;
            }

            switch (mode)
            {
                case CompositeMode.All:
                    return allTrue;
                case CompositeMode.Any:
                    return anyTrue;
                case CompositeMode.None:
                    return !anyTrue;
                default:
                    return false;
            }
        }
    }
}
