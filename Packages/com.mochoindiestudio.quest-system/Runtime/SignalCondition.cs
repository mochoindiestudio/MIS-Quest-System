using System;
using UnityEngine;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Passes once <see cref="RequiredCount"/> matching game signals have been reported via
    /// <see cref="QuestSignals.Report"/> while the owning objective is active. A signal matches when
    /// its event id equals <see cref="EventId"/> and -- if <see cref="Payload"/> is non-empty -- its
    /// payload equals <see cref="Payload"/>. This one type covers "kill 10 wolves", "reach the
    /// bridge", "talk to Giorgio", "press WASD": the consuming game decides what raises each signal.
    ///
    /// Field names match the MIS Dialog System's <c>DialogEventTrigger</c> on purpose, so a dialog
    /// response event forwards to <see cref="QuestSignals.Report"/> with no translation.
    ///
    /// Counting needs an objective to track progress against, so used as a bare
    /// <see cref="Quest.AdvancedUnlock"/> prerequisite it never passes.
    /// </summary>
    [Serializable]
    public sealed class SignalCondition : QuestCondition
    {
        [Tooltip("The signal id to listen for, e.g. \"enemy_killed\" or \"input.move\".")]
        [SerializeField]
        private string eventId;

        [Tooltip("Optional. When set, only signals carrying this exact payload count.")]
        [SerializeField]
        private string payload;

        [Tooltip("How many matching signals complete the objective.")]
        [Min(1)]
        [SerializeField]
        private int requiredCount = 1;

        /// <summary>Signal id this condition listens for.</summary>
        public string EventId
        {
            get => eventId;
            set => eventId = value;
        }

        /// <summary>Optional exact payload filter; empty means "any payload".</summary>
        public string Payload
        {
            get => payload;
            set => payload = value;
        }

        /// <summary>Matching signals needed to pass. Always at least 1.</summary>
        public int RequiredCount
        {
            get => Mathf.Max(1, requiredCount);
            set => requiredCount = Mathf.Max(1, value);
        }

        /// <inheritdoc />
        public override int GetProgressTarget()
        {
            return RequiredCount;
        }

        /// <inheritdoc />
        public override bool Evaluate(in QuestConditionContext context)
        {
            return context.Objective != null && context.Objective.CurrentCount >= RequiredCount;
        }

        /// <inheritdoc />
        public override bool HandleSignal(in QuestConditionContext context, string signalId, string signalPayload, int amount)
        {
            if (context.Objective == null || amount <= 0)
            {
                return false;
            }

            if (!string.Equals(signalId, eventId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(payload) && !string.Equals(signalPayload, payload, StringComparison.Ordinal))
            {
                return false;
            }

            int target = RequiredCount;
            int updated = Mathf.Clamp(context.Objective.CurrentCount + amount, 0, target);
            if (updated == context.Objective.CurrentCount)
            {
                return false;
            }

            context.Objective.CurrentCount = updated;
            return true;
        }
    }
}
