using MochoIndieStudio.QuestSystem;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MochoIndieStudio.QuestSystem.Editor
{
    /// <summary>
    /// View for the quest's root node: title, description, repeatable / time-limit settings, and the
    /// prerequisite and fail-condition lists. Its single output port fans out to every objective node.
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
            extensionContainer.Add(new PropertyField(serializedQuest.FindProperty("prerequisites"), "Prerequisites (all must pass)"));
            extensionContainer.Add(new PropertyField(serializedQuest.FindProperty("failConditions"), "Fail Conditions (any fails the quest)"));

            ObjectivesPort = CreatePort(Direction.Output, Port.Capacity.Multi, quest);
            ObjectivesPort.portName = "Objectives";
            outputContainer.Add(ObjectivesPort);

            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
