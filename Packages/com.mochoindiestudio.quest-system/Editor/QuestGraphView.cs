using System.Collections.Generic;
using System.Linq;
using MochoIndieStudio.QuestSystem;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MochoIndieStudio.QuestSystem.Editor
{
    /// <summary>
    /// Renders and edits a single <see cref="Quest"/>: its root node plus one node per
    /// <see cref="Objective"/>, connected by cosmetic edges (an objective always belongs to its
    /// quest, so those edges are not user-editable). All field editing goes through
    /// <see cref="UnityEditor.SerializedObject"/> bindings, so undo and the Inspector stay in sync.
    /// </summary>
    public sealed class QuestGraphView : GraphView, IQuestGraphCanvas
    {
        /// <summary>Editor canvas grid pitch, in canvas pixels. Matches <c>--spacing</c> in QuestGraphView.uss.</summary>
        public const float GridSpacing = 36f;

        private const float NewNodeSpacing = 40f;
        private const float FirstObjectiveOffset = 360f;
        private const string StyleSheetPath = "Packages/com.mochoindiestudio.quest-system/Editor/QuestGraphView.uss";

        private readonly Quest quest;
        private readonly SerializedObject serializedQuest;

        private QuestRootNodeView rootView;
        private bool didFrameOrigin;

        /// <summary>When true, node positions are quantised to <see cref="GridSpacing"/> as they move.</summary>
        public bool SnapToGrid { get; set; }

        public QuestGraphView(Quest quest)
        {
            this.quest = quest;
            serializedQuest = new SerializedObject(quest);

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

            nodeCreationRequest = _ => CreateObjective();
            graphViewChanged += OnGraphViewChanged;

            Rebuild();

            RegisterCallback<GeometryChangedEvent>(FrameOriginOnce);
        }

        /// <summary>Rebuilds every node and edge from the quest data. Safe to call after any
        /// structural change -- positions live in the data, so nothing visual is lost.</summary>
        private void Rebuild()
        {
            DeleteElements(graphElements.ToList());

            serializedQuest.Update();

            rootView = new QuestRootNodeView(quest, serializedQuest, this);
            AddElement(rootView);

            SerializedProperty objectivesProp = serializedQuest.FindProperty("objectives");

            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                Objective objective = quest.Objectives[i];
                if (objective == null)
                {
                    continue;
                }

                SerializedProperty element = objectivesProp.GetArrayElementAtIndex(i);
                var objectiveView = new ObjectiveNodeView(objective, element, this);
                AddElement(objectiveView);

                Edge edge = rootView.ObjectivesPort.ConnectTo(objectiveView.InputPort);
                edge.SetEnabled(false);
                AddElement(edge);
            }

            this.Bind(serializedQuest);
        }

        private void FrameOriginOnce(GeometryChangedEvent evt)
        {
            if (didFrameOrigin || layout.width <= 0f || layout.height <= 0f)
            {
                return;
            }

            didFrameOrigin = true;
            UnregisterCallback<GeometryChangedEvent>(FrameOriginOnce);
            UpdateViewTransform(new Vector3(layout.width * 0.35f, layout.height * 0.5f, 0f), Vector3.one);
        }

        /// <summary>Appends a new objective to the quest and rebuilds.</summary>
        public void CreateObjective()
        {
            Undo.RecordObject(quest, "Add Objective");

            var objective = new Objective { EditorPosition = NextObjectivePosition() };
            quest.Objectives.Add(objective);

            EditorUtility.SetDirty(quest);
            serializedQuest.Update();
            Rebuild();
        }

        private Vector2 NextObjectivePosition()
        {
            if (quest.Objectives.Count == 0)
            {
                return new Vector2(quest.EditorPosition.x + FirstObjectiveOffset, quest.EditorPosition.y);
            }

            float lowest = float.NegativeInfinity;
            Vector2 anchor = quest.EditorPosition + new Vector2(FirstObjectiveOffset, 0f);

            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                Objective existing = quest.Objectives[i];
                if (existing != null && existing.EditorPosition.y > lowest)
                {
                    lowest = existing.EditorPosition.y;
                    anchor = new Vector2(existing.EditorPosition.x, existing.EditorPosition.y + NewNodeSpacing + 120f);
                }
            }

            return anchor;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            // Objective membership is structural, not user-wired -- offer no connection targets.
            return new List<Port>();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            bool removedObjective = false;

            if (change.elementsToRemove != null)
            {
                foreach (GraphElement element in change.elementsToRemove)
                {
                    if (element is ObjectiveNodeView objectiveView)
                    {
                        Undo.RecordObject(quest, "Delete Objective");
                        quest.Objectives.Remove(objectiveView.Model);
                        removedObjective = true;
                    }
                }
            }

            if (removedObjective)
            {
                EditorUtility.SetDirty(quest);
                serializedQuest.Update();
                // Defer the rebuild until after GraphView finishes applying this change set.
                schedule.Execute(Rebuild).ExecuteLater(0);
            }

            return change;
        }

        /// <summary>Flags the backing <see cref="Quest"/> asset as needing to be saved.</summary>
        public void MarkDirty()
        {
            EditorUtility.SetDirty(quest);
        }
    }
}
