namespace MochoIndieStudio.QuestSystem.Editor
{
    /// <summary>
    /// The slice of a graph view that <see cref="QuestGraphNodeView"/> needs: whether node moves
    /// snap to the grid, and a way to flag the backing asset dirty. Implemented by both
    /// <see cref="QuestGraphView"/> (single quest) and <see cref="QuestListGraphView"/> (quest list).
    /// </summary>
    public interface IQuestGraphCanvas
    {
        /// <summary>When true, node positions are quantised to <see cref="QuestGraphView.GridSpacing"/> as they move.</summary>
        bool SnapToGrid { get; }

        /// <summary>Flags the backing asset(s) as needing to be saved.</summary>
        void MarkDirty();
    }
}
