using MochoIndieStudio.QuestSystem;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MochoIndieStudio.QuestSystem.Editor
{
    /// <summary>
    /// Dedicated editor window hosting the <see cref="QuestGraphView"/> for a single
    /// <see cref="Quest"/> asset: the quest's root node plus one node per objective. Opens on
    /// double-clicking a <see cref="Quest"/> asset.
    /// </summary>
    public sealed class QuestGraphEditorWindow : EditorWindow
    {
        private Quest quest;
        private QuestGraphView graphView;

        [OnOpenAsset]
        private static bool OnOpenQuest(int instanceId, int line)
        {
            // The [OnOpenAsset] signature is fixed to int instanceId by Unity, so EntityIdToObject
            // reports a CS0618 obsolete warning under Unity 6's EntityId type. Harmless -- do not
            // swap in the actually-deprecated InstanceIDToObject.
            var asset = EditorUtility.EntityIdToObject(instanceId) as Quest;
            if (asset == null)
            {
                return false;
            }

            Open(asset);
            return true;
        }

        /// <summary>Opens (or focuses) the window on <paramref name="quest"/>.</summary>
        public static void Open(Quest quest)
        {
            var window = GetWindow<QuestGraphEditorWindow>();
            window.titleContent = new GUIContent("Quest Graph");
            window.Bind(quest);
        }

        private void Bind(Quest target)
        {
            quest = target;
            rootVisualElement.Clear();

            if (quest == null)
            {
                rootVisualElement.Add(new Label("Open a Quest asset to edit it here.") { style = { marginTop = 8, marginLeft = 8 } });
                return;
            }

            titleContent = new GUIContent(quest.name);

            rootVisualElement.Add(BuildToolbar());

            graphView = new QuestGraphView(quest) { name = "Quest Graph View" };
            graphView.style.flexGrow = 1;
            rootVisualElement.Add(graphView);
        }

        private Toolbar BuildToolbar()
        {
            var toolbar = new Toolbar();

            var questLabel = new ToolbarButton(() => Selection.activeObject = quest) { text = quest.name };
            toolbar.Add(questLabel);

            var addObjective = new ToolbarButton(() => graphView?.CreateObjective()) { text = "Add Objective" };
            toolbar.Add(addObjective);

            var spacer = new ToolbarSpacer { flex = true };
            toolbar.Add(spacer);

            var snapToggle = new ToolbarToggle { text = "Snap to Grid" };
            snapToggle.value = graphView != null && graphView.SnapToGrid;
            snapToggle.RegisterValueChangedCallback(evt =>
            {
                if (graphView != null)
                {
                    graphView.SnapToGrid = evt.newValue;
                }
            });
            toolbar.Add(snapToggle);

            return toolbar;
        }

        private void OnEnable()
        {
            if (quest != null)
            {
                Bind(quest);
            }
        }
    }
}
