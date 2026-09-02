using MochoIndieStudio.QuestSystem;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MochoIndieStudio.QuestSystem.Editor
{
    /// <summary>
    /// View for the quest's root node: title, description, repeatable / time-limit settings, the
    /// unlock mode and the optional advanced-unlock condition. The prerequisite quest *links* are
    /// edited in the Quest List graph window (there are no other quest nodes on this canvas) -- here
    /// they show as a read-only summary. The single output port fans out to every objective node.
    /// </summary>
    public sealed class QuestRootNodeView : QuestGraphNodeView
    {
        private const string IconPath = "Packages/com.mochoindiestudio.quest-system/Editor/Icons/icon_quest.png";

        /// <summary>Output port every objective node connects back to.</summary>
        public Port ObjectivesPort { get; }

        public QuestRootNodeView(Quest quest, SerializedObject serializedQuest, QuestGraphView graph)
            : base(graph,
                   quest.EditorPosition,
                   () => quest.EditorWidth,
                   position => quest.EditorPosition = position,
                   width => quest.EditorWidth = width)
        {
            title = "Quest (Root)";
            AddToClassList("quest-root-node");
            SetHeaderIcon(IconPath);

            // The root is never deleted from its own graph.
            capabilities &= ~(Capabilities.Deletable | Capabilities.Copiable);

            extensionContainer.Add(new PropertyField(serializedQuest.FindProperty("title"), "Title"));
            extensionContainer.Add(MultilineField(serializedQuest.FindProperty("description"), "Description"));

            var flags = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            flags.Add(new PropertyField(serializedQuest.FindProperty("repeatable"), "Repeatable"));
            extensionContainer.Add(flags);

            extensionContainer.Add(new PropertyField(serializedQuest.FindProperty("timeLimitSeconds"), "Time Limit (s, 0 = none)"));

            extensionContainer.Add(BuildUnlockedBySummary(quest));
            extensionContainer.Add(new PropertyField(serializedQuest.FindProperty("unlockMode"), "Unlock Mode"));
            extensionContainer.Add(new PropertyField(serializedQuest.FindProperty("advancedUnlock"), "Advanced Unlock (optional)"));

            ObjectivesPort = CreatePort(Direction.Output, Port.Capacity.Multi, quest);
            ObjectivesPort.portName = "Objectives";
            outputContainer.Add(ObjectivesPort);

            RefreshExpandedState();
            RefreshPorts();
        }

        /// <summary>A read-only line listing the quests that unlock this one, plus a pointer to where
        /// those links are edited.</summary>
        private static Label BuildUnlockedBySummary(Quest quest)
        {
            var names = new System.Text.StringBuilder();
            for (int i = 0; i < quest.UnlockedBy.Count; i++)
            {
                Quest prerequisite = quest.UnlockedBy[i];
                if (prerequisite == null)
                {
                    continue;
                }

                if (names.Length > 0)
                {
                    names.Append(", ");
                }

                names.Append(string.IsNullOrEmpty(prerequisite.Title) ? prerequisite.name : prerequisite.Title);
            }

            string summary = names.Length > 0
                ? $"Unlocked by: {names}  ·  edit links in the Quest List graph"
                : "Unlocked by: nothing  ·  add links in the Quest List graph";

            return new Label(summary)
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    opacity = 0.7f,
                    marginTop = 4,
                    marginBottom = 2,
                    marginLeft = 3
                }
            };
        }
    }
}
