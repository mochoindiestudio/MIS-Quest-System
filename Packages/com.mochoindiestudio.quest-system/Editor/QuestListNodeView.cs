using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MochoIndieStudio.QuestSystem.Editor
{
    /// <summary>
    /// A compact node for one <see cref="Quest"/> inside the Quest List graph: its title, objective
    /// count and unlock mode. Its "Unlocks" output port connects to the "Requires" input port of
    /// every quest it unlocks -- those edges are the quest's prerequisite links. Double-click opens
    /// the quest's own graph window.
    /// </summary>
    public sealed class QuestListNodeView : QuestGraphNodeView
    {
        private const string IconPath = "Packages/com.mochoindiestudio.quest-system/Editor/Icons/icon_quest.png";

        /// <summary>The quest this node represents.</summary>
        public Quest Quest { get; }

        /// <summary>Input port -- edges arriving here are quests this one requires.</summary>
        public Port InputPort { get; }

        /// <summary>Output port -- edges leaving here are quests this one unlocks.</summary>
        public Port OutputPort { get; }

        public QuestListNodeView(Quest quest, QuestListGraphView graph)
            : base(graph,
                   graph.GetNodePosition(quest),
                   () => 0f,
                   position => graph.PersistNodePosition(quest, position),
                   _ => { })
        {
            Quest = quest;
            title = string.IsNullOrEmpty(quest.Title) ? quest.name : quest.Title;
            SetHeaderIcon(IconPath);

            InputPort = CreatePort(Direction.Input, Port.Capacity.Multi, quest);
            InputPort.portName = "Requires";
            inputContainer.Add(InputPort);

            OutputPort = CreatePort(Direction.Output, Port.Capacity.Multi, quest);
            OutputPort.portName = "Unlocks";
            outputContainer.Add(OutputPort);

            int objectiveCount = quest.Objectives != null ? quest.Objectives.Count : 0;
            extensionContainer.Add(new Label($"{objectiveCount} objective{(objectiveCount == 1 ? "" : "s")}")
            {
                style = { opacity = 0.7f, marginLeft = 3, marginTop = 2, marginBottom = 2 }
            });

            var serializedQuest = new SerializedObject(quest);
            var unlockMode = new PropertyField(serializedQuest.FindProperty("unlockMode"), "Match");
            unlockMode.Bind(serializedQuest);
            extensionContainer.Add(unlockMode);

            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && evt.button == 0)
                {
                    QuestGraphEditorWindow.Open(quest);
                    evt.StopPropagation();
                }
            });

            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
