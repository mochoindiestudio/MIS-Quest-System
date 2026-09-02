using System.Collections.Generic;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// The global entry point a game uses to tell the quest system that something happened:
    /// <c>QuestSignals.Report("enemy_killed", "wolf")</c>. Every live <see cref="QuestLog"/> receives
    /// the report and advances any matching <see cref="SignalCondition"/> objective.
    ///
    /// This is the one static in the package. It is a stateless forwarder -- it holds no quest data,
    /// only the set of live logs, each of which adds and removes itself. A game normally has exactly
    /// one <see cref="QuestLog"/>, so "broadcast to all" behaves like "send to the log".
    /// </summary>
    public static class QuestSignals
    {
        private static readonly List<QuestLog> Logs = new List<QuestLog>();

        /// <summary>
        /// Reports a game signal to every live <see cref="QuestLog"/>.
        /// </summary>
        /// <param name="eventId">Signal identifier, matched against <see cref="SignalCondition.EventId"/>.</param>
        /// <param name="payload">Optional data; a signal objective with a non-empty payload only matches an equal one.</param>
        /// <param name="amount">Progress to add (defaults to 1). Values &lt;= 0 are ignored.</param>
        public static void Report(string eventId, string payload = null, int amount = 1)
        {
            if (string.IsNullOrEmpty(eventId) || amount <= 0)
            {
                return;
            }

            // Iterate a copy: a log's signal handling may register or dispose another log.
            int count = Logs.Count;
            if (count == 0)
            {
                return;
            }

            var snapshot = new QuestLog[count];
            Logs.CopyTo(snapshot);

            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].Report(eventId, payload, amount);
            }
        }

        internal static void Register(QuestLog log)
        {
            if (log != null && !Logs.Contains(log))
            {
                Logs.Add(log);
            }
        }

        internal static void Unregister(QuestLog log)
        {
            Logs.Remove(log);
        }
    }
}
