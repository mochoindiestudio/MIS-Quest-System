using System.Collections.Generic;
using MochoIndieStudio.QuestSystem;
using TMPro;
using UnityEngine;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// The quest book (TAB). Lists the demo quest's objectives, each drawn by an
    /// <see cref="ObjectiveRowView"/> cloned from a disabled template.
    /// </summary>
    public sealed class QuestLogView : MonoBehaviour
    {
        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private TMP_Text titleLabel;

        [SerializeField]
        private RectTransform rowContainer;

        [SerializeField]
        private ObjectiveRowView rowTemplate;

        [SerializeField]
        private DemoGame game;

        private readonly List<ObjectiveRowView> rows = new List<ObjectiveRowView>();

        /// <summary>Whether the panel is currently shown.</summary>
        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (rowTemplate != null)
            {
                rowTemplate.gameObject.SetActive(false);
            }

            SetOpen(false);
        }

        /// <summary>Flips the panel open/closed.</summary>
        public void Toggle() => SetOpen(!IsOpen);

        /// <summary>Shows or hides the panel; a fresh <see cref="Refresh"/> runs when it opens.</summary>
        public void SetOpen(bool open)
        {
            if (panel != null)
            {
                panel.SetActive(open);
            }

            if (open)
            {
                Refresh();
            }
        }

        /// <summary>Re-reads objective state and repaints the rows.</summary>
        public void Refresh()
        {
            if (game == null || game.Log == null || rowTemplate == null)
            {
                return;
            }

            QuestHandle quest = game.Log.Get(game.MainQuestId);
            if (quest == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = quest.Title;
            }

            int shown = 0;
            IReadOnlyList<ObjectiveHandle> objectives = quest.Objectives;
            for (int i = 0; i < objectives.Count; i++)
            {
                ObjectiveHandle objective = objectives[i];
                if (objective.IsHidden && objective.State == ObjectiveState.Inactive)
                {
                    continue;
                }

                ObjectiveRowView row = shown < rows.Count ? rows[shown] : CreateRow();
                row.gameObject.SetActive(true);
                row.Bind(objective);
                shown++;
            }

            for (int i = shown; i < rows.Count; i++)
            {
                rows[i].gameObject.SetActive(false);
            }
        }

        private ObjectiveRowView CreateRow()
        {
            ObjectiveRowView row = Instantiate(rowTemplate, rowContainer);
            rows.Add(row);
            return row;
        }
    }
}
