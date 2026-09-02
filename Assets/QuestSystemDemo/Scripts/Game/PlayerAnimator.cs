using UnityEngine;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// Code-only sprite swapping for the player's 4-direction walk. No <see cref="Animator"/> asset:
    /// it reads <see cref="PlayerController.CurrentMove"/>, picks a facing, and cycles the four walk
    /// frames from <see cref="DemoSpriteMap"/> while moving (frame 0 when idle).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerAnimator : MonoBehaviour
    {
        [SerializeField]
        private PlayerController player;

        [SerializeField]
        private DemoSpriteMap spriteMap;

        [SerializeField]
        private float framesPerSecond = 8f;

        [SerializeField]
        private float moveThreshold = 0.05f;

        private SpriteRenderer spriteRenderer;
        private Facing facing = Facing.Down;
        private float animTime;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (spriteMap == null || player == null)
            {
                return;
            }

            Vector2 move = player.CurrentMove;
            bool moving = move.sqrMagnitude > moveThreshold * moveThreshold;

            if (moving)
            {
                facing = FacingFrom(move);
                animTime += Time.deltaTime * framesPerSecond;
            }
            else
            {
                animTime = 0f;
            }

            Sprite[] frames = spriteMap.player.Frames(facing);
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            int index = moving ? Mathf.FloorToInt(animTime) % frames.Length : 0;
            Sprite sprite = frames[index] != null ? frames[index] : frames[0];
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        private static Facing FacingFrom(Vector2 v)
        {
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            {
                return v.x < 0f ? Facing.Left : Facing.Right;
            }

            return v.y < 0f ? Facing.Down : Facing.Up;
        }
    }
}
