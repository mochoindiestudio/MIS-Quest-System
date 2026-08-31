namespace MochoIndieStudio.QuestSystem
{
    /// <summary>How a <see cref="CompositeCondition"/> combines the results of its child conditions.</summary>
    public enum CompositeMode
    {
        /// <summary>True only when every child is true (an empty list is true).</summary>
        All = 0,

        /// <summary>True when at least one child is true (an empty list is false).</summary>
        Any = 1,

        /// <summary>True when no child is true (logical NOR; an empty list is true).</summary>
        None = 2
    }
}
