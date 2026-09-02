using MochoIndieStudio.QuestSystem;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MochoIndieStudio.QuestSystem.Editor
{
    /// <summary>
    /// View for one <see cref="Objective"/>: its description, required / hidden / stage settings, its
    /// complete-when condition and its optional fail-when guard (each a <see cref="QuestCondition"/>
    /// chosen from the field's type dropdown). Deleting the node removes the objective from the quest.
    /// </summary>
    public sealed class ObjectiveNodeView : QuestGraphNodeView
    {
        private const string IconPath = "Packages/com.mochoindiestudio.quest-system/Editor/Icons/icon_objective.png";

        /// <summary>The objective this node edits.</summary>
        public Objective Model { get; }

        /// <summary>Input port connected back to the quest root's objectives port.</summary>
        public Port InputPort { get; }

        public ObjectiveNodeView(Objective objective, SerializedProperty serializedObjective, QuestGraphView graph)
            : base(graph,
                   objective.EditorPosition,
                   () => objective.EditorWidth,
                   position => objective.EditorPosition = position,
                   width => objective.EditorWidth = width)
        {
            Model = objective;
            title = "Objective";
            SetHeaderIcon(IconPath);

            InputPort = CreatePort(Direction.Input, Port.Capacity.Single, objective);
            InputPort.portName = "Quest";
            inputContainer.Add(InputPort);

            extensionContainer.Add(MultilineField(serializedObjective.FindPropertyRelative("description"), "Description"));

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row.Add(new PropertyField(serializedObjective.FindPropertyRelative("required"), "Required"));
            row.Add(new PropertyField(serializedObjective.FindPropertyRelative("hidden"), "Hidden"));
            row.Add(new PropertyField(serializedObjective.FindPropertyRelative("stage"), "Stage"));
            extensionContainer.Add(row);

            extensionContainer.Add(new PropertyField(serializedObjective.FindPropertyRelative("completeWhen"), "Complete When"));
            extensionContainer.Add(new PropertyField(serializedObjective.FindPropertyRelative("failWhen"), "Fail When (optional)"));

            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
