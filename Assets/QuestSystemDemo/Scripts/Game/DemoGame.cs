using MochoIndieStudio.QuestSystem;
using UnityEngine;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// Bootstraps the quest system for the demo: owns the <see cref="QuestLog"/>, registers the
    /// quest list, pumps <see cref="QuestLog.Tick"/>, holds the one game-side rule (crafting) and
    /// grants the completion reward. The HUD reads <see cref="Log"/> and subscribes to its events.
    /// </summary>
    public sealed class DemoGame : MonoBehaviour
    {
        [SerializeField]
        private QuestList questList;

        [SerializeField]
        private Inventory inventory;

        [SerializeField]
        private ItemDefinition bucketOnRope;

        [SerializeField]
        private ItemDefinition bucketOfWater;

        /// <summary>The live quest log. Created in <c>Awake</c>, so it exists before any <c>OnEnable</c>.</summary>
        public QuestLog Log { get; private set; }

        /// <summary>The id of the demo's single quest (the first in the list), or null.</summary>
        public string MainQuestId =>
            questList != null && questList.Quests.Count > 0 && questList.Quests[0] != null
                ? questList.Quests[0].Id
                : null;

        private void Awake()
        {
            Log = new QuestLog();
        }

        private void Start()
        {
            Log.OnQuestCompleted += HandleQuestCompleted;
            if (questList != null)
            {
                Log.Register(questList);
            }
        }

        private void Update()
        {
            Log.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (Log != null)
            {
                Log.OnQuestCompleted -= HandleQuestCompleted;
                Log.Dispose();
                Log = null;
            }
        }

        /// <summary>
        /// The demo's sole crafting rule: with a bucket and a rope (and no bucket-on-rope yet),
        /// consume both, add the bucket on a rope, and report <see cref="DemoSignals.ItemCrafted"/>.
        /// </summary>
        public bool TryCraftBucketOnRope()
        {
            if (inventory == null ||
                !inventory.Has(DemoSignals.Bucket) ||
                !inventory.Has(DemoSignals.Rope) ||
                inventory.Has(DemoSignals.BucketOnRope))
            {
                return false;
            }

            inventory.Remove(DemoSignals.Bucket);
            inventory.Remove(DemoSignals.Rope);
            inventory.Add(bucketOnRope);
            QuestSignals.Report(DemoSignals.ItemCrafted, DemoSignals.BucketOnRope);
            return true;
        }

        private void HandleQuestCompleted(QuestHandle quest)
        {
            if (inventory != null && bucketOfWater != null)
            {
                inventory.Add(bucketOfWater);
            }
        }
    }
}
