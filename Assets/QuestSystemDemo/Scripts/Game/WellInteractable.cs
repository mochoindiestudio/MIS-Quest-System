using MochoIndieStudio.Signals;
using UnityEngine;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// Lives on the well's interaction-zone child (a trigger <see cref="CircleCollider2D"/>).
    /// While the player is in the zone it reports <see cref="DemoSignals.Reached"/> / "well" -- on
    /// entry and again every <see cref="ReReportInterval"/> seconds, so "Find the old well" still
    /// completes if that objective only unlocks while the player is already standing here.
    /// Pressing E (when the player holds a bucket on a rope) reports <see cref="DemoSignals.Used"/>.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class WellInteractable : Interactable
    {
        private const float ReReportInterval = 1f;

        private bool playerInRange;
        private float reportTimer;

        public override string Prompt =>
            CanInteract ? "Lower the bucket into the well" : "You need a bucket on a rope";

        public override bool CanInteract =>
            Inventory != null && Inventory.Has(DemoSignals.BucketOnRope);

        public override void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            // You lower the bucket on a rope into the well -- it's consumed here (game/inventory
            // concern); the game grants the Bucket of Water as the quest-completion reward.
            Inventory.Remove(DemoSignals.BucketOnRope);
            MisSignals.Report(DemoSignals.Used, DemoSignals.Well);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            playerInRange = true;
            reportTimer = 0f;
            MisSignals.Report(DemoSignals.Reached, DemoSignals.Well);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null)
            {
                playerInRange = false;
            }
        }

        private void Update()
        {
            if (!playerInRange)
            {
                return;
            }

            reportTimer += Time.deltaTime;
            if (reportTimer >= ReReportInterval)
            {
                reportTimer = 0f;
                MisSignals.Report(DemoSignals.Reached, DemoSignals.Well);
            }
        }
    }
}
