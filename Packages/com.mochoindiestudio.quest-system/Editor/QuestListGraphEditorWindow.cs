using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MochoIndieStudio.QuestSystem.Editor
{
    /// <summary>
    /// Dedicated editor window hosting the <see cref="QuestListGraphView"/> for a single
    /// <see cref="QuestList"/> asset: one node per quest, edges for prerequisite links. Opens on
    /// double-clicking a <see cref="QuestList"/> asset. Add existing quests by dragging them from the
    /// Project window onto the canvas; "New Quest" creates one beside the list.
    /// </summary>
    public sealed class QuestListGraphEditorWindow : EditorWindow
    {
        private QuestList list;
        private QuestListGraphView graphView;

        [OnOpenAsset]
        private static bool OnOpenQuestList(int instanceId, int line)
        {
            // The [OnOpenAsset] signature is fixed to int instanceId by Unity, so EntityIdToObject
            // reports a CS0618 obsolete warning under Unity 6's EntityId type. Harmless -- do not
            // swap in the actually-deprecated InstanceIDToObject.
            var asset = EditorUtility.EntityIdToObject(instanceId) as QuestList;
            if (asset == null)
            {
                return false;
            }

            Open(asset);
            return true;
        }

        /// <summary>Opens (or focuses) the window on <paramref name="questList"/>.</summary>
        public static void Open(QuestList questList)
        {
            var window = GetWindow<QuestListGraphEditorWindow>();
            window.titleContent = new GUIContent("Quest List Graph");
            window.Bind(questList);
        }

        private void Bind(QuestList target)
        {
            list = target;
            rootVisualElement.Clear();

            if (list == null)
            {
                rootVisualElement.Add(new Label("Open a Quest List asset to edit it here.")
                {
                    style = { marginTop = 8, marginLeft = 8 }
                });
                return;
            }

            titleContent = new GUIContent(list.name);
            rootVisualElement.Add(BuildToolbar());

            graphView = new QuestListGraphView(list) { name = "Quest List Graph View" };
            graphView.style.flexGrow = 1;
            rootVisualElement.Add(graphView);
        }

        private Toolbar BuildToolbar()
        {
            var toolbar = new Toolbar();

            toolbar.Add(new ToolbarButton(() => Selection.activeObject = list) { text = list.name });
            toolbar.Add(new ToolbarButton(CreateQuestBesideList) { text = "New Quest" });

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

        private void CreateQuestBesideList()
        {
            string listPath = AssetDatabase.GetAssetPath(list);
            string directory = string.IsNullOrEmpty(listPath) ? "Assets" : Path.GetDirectoryName(listPath);
            string path = AssetDatabase.GenerateUniqueAssetPath(directory + "/New Quest.asset");

            var quest = CreateInstance<Quest>();
            quest.Title = "New Quest";
            AssetDatabase.CreateAsset(quest, path);
            AssetDatabase.SaveAssets();

            graphView?.AddQuest(quest);
            EditorGUIUtility.PingObject(quest);
        }

        private void OnEnable()
        {
            if (list != null)
            {
                Bind(list);
            }
        }
    }
}
