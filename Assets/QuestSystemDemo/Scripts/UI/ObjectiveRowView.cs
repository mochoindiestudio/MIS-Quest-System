using MochoIndieStudio.QuestSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// One line in the quest log. Renders an <see cref="ObjectiveHandle"/> by state:
    /// inactive rows are dimmed (alpha 0.5), the active one is marked with "▸" and an accent colour,
    /// completed ones show a green check-mark icon.
    /// </summary>
    public sealed class ObjectiveRowView : MonoBehaviour
    {
        private static readonly Color ActiveColor = new Color(1f, 0.95f, 0.7f);
        private static readonly Color DoneColor = new Color(0.55f, 0.85f, 0.55f);
        private static readonly Color FailColor = new Color(0.9f, 0.5f, 0.5f);

        [SerializeField]
        private CanvasGroup group;

        [SerializeField]
        private TMP_Text label;

        [Tooltip("Green check-mark icon, shown only when the objective is completed.")]
        [SerializeField]
        private Image checkmark;

        /// <summary>Renders <paramref name="objective"/>.</summary>
        public void Bind(ObjectiveHandle objective)
        {
            string text = objective.Description;
            if (objective.TargetCount > 1)
            {
                text += $" ({objective.CurrentCount}/{objective.TargetCount})";
            }

            bool completed = objective.State == ObjectiveState.Completed;
            if (checkmark != null)
            {
                checkmark.enabled = completed;
            }

            switch (objective.State)
            {
                case ObjectiveState.Completed:
                    Apply(1f, text, DoneColor);
                    break;
                case ObjectiveState.Active:
                    Apply(1f, $"▸ {text}", ActiveColor);
                    break;
                case ObjectiveState.Failed:
                    Apply(1f, $"✘ {text}", FailColor);
                    break;
                default:
                    Apply(0.5f, text, Color.white);
                    break;
            }
        }

        private void Apply(float alpha, string richText, Color color)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }

            if (label != null)
            {
                label.text = richText;
                label.color = color;
            }
        }
    }
}
