using System;
using System.Collections.Generic;
using UnityEngine;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// A titled unit of work: a staged list of <see cref="Objectives"/> plus the conditions that
    /// make it available, the conditions that fail it, an optional time limit and a repeatable flag.
    /// Authored as a standalone asset and collected into one or more <see cref="QuestList"/>s.
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

        [Tooltip("All must pass for the quest to move from Inactive to Available.")]
        [SerializeReference]
        private List<QuestCondition> prerequisites = new List<QuestCondition>();

        [Tooltip("Any passing while the quest is Active fails it.")]
        [SerializeReference]
        private List<QuestCondition> failConditions = new List<QuestCondition>();

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

        /// <summary>Conditions (implicit AND) that gate <see cref="QuestState.Available"/>. Never null.</summary>
        public List<QuestCondition> Prerequisites => prerequisites;

        /// <summary>Conditions (implicit OR) that fail the quest while active. Never null.</summary>
        public List<QuestCondition> FailConditions => failConditions;

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
                objectives[i]?.EnsureId();
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
