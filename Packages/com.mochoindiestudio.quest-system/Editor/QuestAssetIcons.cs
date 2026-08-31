using MochoIndieStudio.QuestSystem;
using UnityEditor;
using UnityEngine;

namespace MochoIndieStudio.QuestSystem.Editor
{
    /// <summary>
    /// Assigns the package's custom Project-window icons to <see cref="Quest"/> / <see cref="QuestList"/>
    /// assets. Done through <see cref="MonoImporter.SetIcon"/> rather than the <c>[Icon]</c> attribute,
    /// which isn't picked up reliably. Idempotent -- runs on every domain reload but only touches the
    /// importer when the icon actually differs.
    /// </summary>
    [InitializeOnLoad]
    internal static class QuestAssetIcons
    {
        private const string ScriptsFolder = "Packages/com.mochoindiestudio.quest-system/Runtime/";
        private const string IconsFolder = "Packages/com.mochoindiestudio.quest-system/Editor/Icons/";

        static QuestAssetIcons()
        {
            ApplyIcon(ScriptsFolder + "Quest.cs", IconsFolder + "icon_quest.png");
            ApplyIcon(ScriptsFolder + "QuestList.cs", IconsFolder + "icon_questlist.png");
        }

        private static void ApplyIcon(string scriptPath, string iconPath)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (script == null || icon == null)
            {
                return;
            }

            var importer = (MonoImporter)AssetImporter.GetAtPath(scriptPath);
            if (importer == null || importer.GetIcon() == icon)
            {
                return;
            }

            importer.SetIcon(icon);
            importer.SaveAndReimport();
        }
    }
}
