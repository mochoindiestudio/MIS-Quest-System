namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// The signal ids, payloads and item ids the demo uses, in one place. Pickups, the well and the
    /// authored <c>WellQuest</c> asset all reference these so a typo can't silently break a step.
    /// </summary>
    /// <remarks>
    /// Not marked <c>[SignalIdProvider]</c>: this class mixes event ids with payload / item-id
    /// constants, and a provider surfaces <em>every</em> <c>public const string</c>. A provider class
    /// should hold signal ids only (see <c>InventorySignalIds</c>).
    /// </remarks>
    public static class DemoSignals
    {
        // Signal event ids (the first argument to MisSignals.Report).
        public const string ItemCollected = "item_collected";
        public const string ItemCrafted = "item_crafted";
        public const string Reached = "reached";
        public const string Used = "used";

        // Item ids (also the payloads for ItemCollected / ItemCrafted).
        public const string Bucket = "bucket";
        public const string Rope = "rope";
        public const string BucketOnRope = "bucket_on_rope";
        public const string BucketOfWater = "bucket_of_water";

        // Payload for the Reached / Used well signals.
        public const string Well = "well";
    }
}
