using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MochoIndieStudio.QuestSystem.Editor
{
    /// <summary>
    /// Renders and edits a <see cref="QuestList"/>: one <see cref="QuestListNodeView"/> per quest,
    /// with edges representing prerequisite links. Dragging an edge from quest A's "Unlocks" port to
    /// quest B's "Requires" port adds A to <c>B.unlockedBy</c>; deleting the edge removes it. Drop
    /// <see cref="Quest"/> assets from the Project window onto the canvas to add them to the list.
    /// </summary>
    public sealed class QuestListGraphView : GraphView, IQuestGraphCanvas
    {
        private const float NodeSpacingX = 260f;
        private const float NodeSpacingY = 170f;
        private const int NodesPerColumn = 4;
        private const string StyleSheetPath = "Packages/com.mochoindiestudio.quest-system/Editor/QuestGraphView.uss";

        private readonly QuestList list;
        private readonly Dictionary<Quest, QuestListNodeView> nodesByQuest = new Dictionary<Quest, QuestListNodeView>();

        private bool didFrameOrigin;

        /// <inheritdoc />
        public bool SnapToGrid { get; set; }

        public QuestListGraphView(QuestList list)
        {
            this.list = list;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            grid.StretchToParentSize();
            Insert(0, grid);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }

            graphViewChanged += OnGraphViewChanged;
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);

            Rebuild();

            RegisterCallback<GeometryChangedEvent>(FrameOriginOnce);
        }

        /// <inheritdoc />
        public void MarkDirty()
        {
            EditorUtility.SetDirty(list);
        }

        /// <summary>Canvas position stored for <paramref name="quest"/>, or a staggered default.</summary>
        public Vector2 GetNodePosition(Quest quest)
        {
            int index = list.Quests.IndexOf(quest);
            Vector2 stored = list.GetNodePosition(index);
            if (stored != Vector2.zero)
            {
                return stored;
            }

            int slot = Mathf.Max(0, index);
            return new Vector2((slot / NodesPerColumn) * NodeSpacingX, (slot % NodesPerColumn) * NodeSpacingY);
        }

        /// <summary>Writes <paramref name="quest"/>'s canvas position back into the list asset.</summary>
        public void PersistNodePosition(Quest quest, Vector2 position)
        {
            int index = list.Quests.IndexOf(quest);
            if (index < 0)
            {
                return;
            }

            list.SetNodePosition(index, position);
            EditorUtility.SetDirty(list);
        }

        private void Rebuild()
        {
            DeleteElements(graphElements.ToList());
            nodesByQuest.Clear();

            for (int i = 0; i < list.Quests.Count; i++)
            {
                Quest quest = list.Quests[i];
                if (quest == null || nodesByQuest.ContainsKey(quest))
                {
                    continue;
                }

                var node = new QuestListNodeView(quest, this);
                AddElement(node);
                nodesByQuest.Add(quest, node);
            }

            foreach (KeyValuePair<Quest, QuestListNodeView> pair in nodesByQuest)
            {
                Quest dependent = pair.Key;
                List<Quest> unlockedBy = dependent.UnlockedBy;
                for (int i = 0; i < unlockedBy.Count; i++)
                {
                    Quest prerequisite = unlockedBy[i];
                    if (prerequisite != null && nodesByQuest.TryGetValue(prerequisite, out QuestListNodeView from))
                    {
                        Edge edge = from.OutputPort.ConnectTo(pair.Value.InputPort);
                        AddElement(edge);
                    }
                }
            }
        }

        private void FrameOriginOnce(GeometryChangedEvent evt)
        {
            if (didFrameOrigin || layout.width <= 0f || layout.height <= 0f)
            {
                return;
            }

            didFrameOrigin = true;
            UnregisterCallback<GeometryChangedEvent>(FrameOriginOnce);
            UpdateViewTransform(new Vector3(layout.width * 0.2f, layout.height * 0.25f, 0f), Vector3.one);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatible = new List<Port>();
            var startQuest = startPort.node as QuestListNodeView;

            ports.ForEach(port =>
            {
                if (port.direction == startPort.direction || port.node == startPort.node)
                {
                    return;
                }

                var otherQuest = port.node as QuestListNodeView;
                if (startQuest == null || otherQuest == null)
                {
                    return;
                }

                // Resolve which quest is the prerequisite and which is the dependent for this pairing.
                Quest prerequisite = startPort.direction == Direction.Output ? startQuest.Quest : otherQuest.Quest;
                Quest dependent = startPort.direction == Direction.Output ? otherQuest.Quest : startQuest.Quest;

                if (!dependent.UnlockedBy.Contains(prerequisite) && !WouldCycle(prerequisite, dependent))
                {
                    compatible.Add(port);
                }
            });

            return compatible;
        }

        /// <summary>True if making <paramref name="dependent"/> require <paramref name="prerequisite"/>
        /// would close a prerequisite loop.</summary>
        private static bool WouldCycle(Quest prerequisite, Quest dependent)
        {
            if (prerequisite == dependent)
            {
                return true;
            }

            var stack = new Stack<Quest>();
            stack.Push(prerequisite);
            var seen = new HashSet<Quest>();

            while (stack.Count > 0)
            {
                Quest current = stack.Pop();
                if (current == null || !seen.Add(current))
                {
                    continue;
                }

                if (current == dependent)
                {
                    return true;
                }

                for (int i = 0; i < current.UnlockedBy.Count; i++)
                {
                    stack.Push(current.UnlockedBy[i]);
                }
            }

            return false;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            bool structuralChange = false;

            if (change.edgesToCreate != null)
            {
                foreach (Edge edge in change.edgesToCreate)
                {
                    var from = edge.output?.node as QuestListNodeView;
                    var to = edge.input?.node as QuestListNodeView;
                    if (from == null || to == null || to.Quest.UnlockedBy.Contains(from.Quest))
                    {
                        continue;
                    }

                    Undo.RecordObject(to.Quest, "Add Prerequisite");
                    to.Quest.UnlockedBy.Add(from.Quest);
                    EditorUtility.SetDirty(to.Quest);
                    structuralChange = true;
                }
            }

            if (change.elementsToRemove != null)
            {
                foreach (GraphElement element in change.elementsToRemove)
                {
                    switch (element)
                    {
                        case Edge edge:
                        {
                            var from = edge.output?.node as QuestListNodeView;
                            var to = edge.input?.node as QuestListNodeView;
                            if (from != null && to != null && to.Quest.UnlockedBy.Contains(from.Quest))
                            {
                                Undo.RecordObject(to.Quest, "Remove Prerequisite");
                                to.Quest.UnlockedBy.Remove(from.Quest);
                                EditorUtility.SetDirty(to.Quest);
                                structuralChange = true;
                            }

                            break;
                        }

                        case QuestListNodeView node:
                        {
                            RemoveQuestFromList(node.Quest);
                            structuralChange = true;
                            break;
                        }
                    }
                }
            }

            if (structuralChange)
            {
                schedule.Execute(Rebuild).ExecuteLater(0);
            }

            return change;
        }

        private void RemoveQuestFromList(Quest quest)
        {
            int index = list.Quests.IndexOf(quest);
            if (index < 0)
            {
                return;
            }

            Undo.RecordObject(list, "Remove Quest From List");
            list.Quests.RemoveAt(index);

            // Drop any prerequisite links pointing at the removed quest from the quests that remain.
            for (int i = 0; i < list.Quests.Count; i++)
            {
                Quest other = list.Quests[i];
                if (other != null && other.UnlockedBy.Remove(quest))
                {
                    EditorUtility.SetDirty(other);
                }
            }

            EditorUtility.SetDirty(list);
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (DraggedQuests().Any())
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            }
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            Quest[] dropped = DraggedQuests().ToArray();
            if (dropped.Length == 0)
            {
                return;
            }

            DragAndDrop.AcceptDrag();
            Vector2 canvasPoint = contentViewContainer.WorldToLocal(evt.mousePosition);

            Undo.RecordObject(list, "Add Quest To List");
            int added = 0;
            foreach (Quest quest in dropped)
            {
                if (list.Quests.Contains(quest))
                {
                    continue;
                }

                list.Quests.Add(quest);
                list.SetNodePosition(list.Quests.Count - 1, canvasPoint + new Vector2(added * 30f, added * 30f));
                added++;
            }

            if (added > 0)
            {
                EditorUtility.SetDirty(list);
                Rebuild();
            }
        }

        private static IEnumerable<Quest> DraggedQuests()
        {
            Object[] refs = DragAndDrop.objectReferences;
            for (int i = 0; i < refs.Length; i++)
            {
                if (refs[i] is Quest quest)
                {
                    yield return quest;
                }
            }
        }

        /// <summary>Adds an existing quest to the list at the canvas origin. Used by the toolbar.</summary>
        public void AddQuest(Quest quest)
        {
            if (quest == null || list.Quests.Contains(quest))
            {
                return;
            }

            Undo.RecordObject(list, "Add Quest To List");
            list.Quests.Add(quest);
            EditorUtility.SetDirty(list);
            Rebuild();
        }
    }
}
