using System;
using System.Collections.Generic;
using UnityEngine;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// A titled unit of work: a staged list of <see cref="Objectives"/> plus the quests that unlock
    /// it (<see cref="UnlockedBy"/>), an optional extra unlock condition, an optional time limit and
    /// a repeatable flag. Authored as a standalone asset and collected into one or more
    /// <see cref="QuestList"/>s. Failure is objective-level: a <c>Required</c> objective failing (or
    /// the time limit) fails the quest.
    ///
    /// The package holds no reward data -- a game grants rewards from its
    /// <see cref="QuestLog.OnQuestCompleted"/> handler.
    /// </summary>
    [CreateAssetMenu(fileName = "New Quest", menuName = "MIS Quest System/Quest")]
    public sealed class Quest : ScriptableObject
    {
        /// <summary>Sentinel <see cref="TimeLimitSeconds"/> value meaning "no time limit".</summary>
        public const float NoTimeLimit = 0f;

        [SerializeField]
        private string id;

        [SerializeField]
        private string title;

        [TextArea(2, 6)]
        [SerializeField]
        private string description;

        [SerializeField]
        private List<Objective> objectives = new List<Objective>();

        [Tooltip("Quests that must be Completed before this one becomes Available. Edit these links " +
                 "by connecting nodes in the Quest List graph window. Empty = available immediately.")]
        [SerializeField]
        private List<Quest> unlockedBy = new List<Quest>();

        [Tooltip("Whether every quest in Unlocked By must be Completed, or just one of them.")]
        [SerializeField]
        private PrerequisiteMode unlockMode = PrerequisiteMode.All;

        [Tooltip("Optional extra gate, ANDed with the Unlocked By check. Use a Composite condition " +
                 "for anything the link graph can't express (non-Completed states, game conditions).")]
        [SerializeReference]
        private QuestCondition advancedUnlock;

        [Tooltip("Seconds of accumulated Active time before the quest fails. 0 = no limit.")]
        [Min(0f)]
        [SerializeField]
        private float timeLimitSeconds = NoTimeLimit;

        [Tooltip("If set, the quest can be reset from a finished state and run again.")]
        [SerializeField]
        private bool repeatable;

        [SerializeField]
        private Vector2 editorPosition;

        [SerializeField]
        private float editorWidth;

        /// <summary>Stable identifier -- referenced by <see cref="QuestStateCondition"/>, save snapshots and the runtime API.</summary>
        public string Id => id;

        /// <summary>Short player-facing name.</summary>
        public string Title
        {
            get => title;
            set => title = value;
        }

        /// <summary>Longer player-facing description / journal blurb.</summary>
        public string Description
        {
            get => description;
            set => description = value;
        }

        /// <summary>The quest's objectives, in author order. Never null; may be empty.</summary>
        public List<Objective> Objectives => objectives;

        /// <summary>Quests that must be <see cref="QuestState.Completed"/> for this one to become
        /// <see cref="QuestState.Available"/>, combined per <see cref="UnlockMode"/>. Never null;
        /// entries may be null. Edited as edges in the Quest List graph window.</summary>
        public List<Quest> UnlockedBy => unlockedBy;

        /// <summary>Whether all of <see cref="UnlockedBy"/> or just one must be completed.</summary>
        public PrerequisiteMode UnlockMode => unlockMode;

        /// <summary>Optional extra unlock gate, ANDed with the <see cref="UnlockedBy"/> check. May be null.</summary>
        public QuestCondition AdvancedUnlock => advancedUnlock;

        /// <summary>Accumulated active-time limit in seconds; <see cref="NoTimeLimit"/> when unlimited.</summary>
        public float TimeLimitSeconds => Mathf.Max(0f, timeLimitSeconds);

        /// <summary>Whether <see cref="QuestLog.ResetQuest"/> is allowed on this quest.</summary>
        public bool Repeatable => repeatable;

        /// <summary>True when <see cref="TimeLimitSeconds"/> imposes a real limit.</summary>
        public bool HasTimeLimit => TimeLimitSeconds > 0f;

        /// <summary>Graph-editor canvas position of the quest's root node. Editor-only concern,
        /// stored with the data so no separate layout file is needed.</summary>
        public Vector2 EditorPosition
        {
            get => editorPosition;
            set => editorPosition = value;
        }

        /// <summary>Graph-editor root-node width in canvas pixels; 0 means "auto-fit to content".</summary>
        public float EditorWidth
        {
            get => editorWidth;
            set => editorWidth = value;
        }

        /// <summary>Finds an objective by its <see cref="Objective.Id"/>, or null. Manual loop -- no LINQ on this path.</summary>
        public Objective GetObjective(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId))
            {
                return null;
            }

            for (int i = 0; i < objectives.Count; i++)
            {
                if (objectives[i] != null && objectives[i].Id == objectiveId)
                {
                    return objectives[i];
                }
            }

            return null;
        }

        private void Awake()
        {
            EnsureId();
        }

        private void OnValidate()
        {
            EnsureId();

            for (int i = 0; i < objectives.Count; i++)
            {
                Objective objective = objectives[i];
                if (objective == null)
                {
                    continue;
                }

                objective.EnsureId();

                // An objective added through the Inspector's list "+" gets a null completion
                // (SerializedProperty resize does not run the field initializer). Give it the common
                // default so it is usable straight away; the graph editor already does this.
                if (objective.CompleteWhen == null)
                {
                    objective.CompleteWhen = new SignalCondition();
                }
            }
        }

        private void EnsureId()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
            }
        }
    }
}
