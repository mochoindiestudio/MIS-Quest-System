using System.Collections.Generic;
using UnityEngine;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// Top-down 2D movement plus interaction. Every <see cref="Interactable"/> whose trigger the
    /// player overlaps goes in a set; the nearest one drives the on-screen prompt and receives the
    /// interact key.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField]
        private float moveSpeed = 4f;

        [SerializeField]
        private DemoInput input;

        [SerializeField]
        private DemoHud hud;

        private readonly List<Interactable> inRange = new List<Interactable>();
        private Rigidbody2D body;
        private Interactable current;

        /// <summary>The latest movement input, for <see cref="PlayerAnimator"/> to read.</summary>
        public Vector2 CurrentMove { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        private void OnEnable()
        {
            if (input != null)
            {
                input.InteractPressed += OnInteract;
            }
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.InteractPressed -= OnInteract;
            }
        }

        private void Update()
        {
            CurrentMove = input != null ? Vector2.ClampMagnitude(input.Move, 1f) : Vector2.zero;
            UpdateNearest();
        }

        private void FixedUpdate()
        {
            body.MovePosition(body.position + CurrentMove * (moveSpeed * Time.fixedDeltaTime));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Interactable interactable = other.GetComponentInParent<Interactable>();
            if (interactable != null && !inRange.Contains(interactable))
            {
                inRange.Add(interactable);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Interactable interactable = other.GetComponentInParent<Interactable>();
            if (interactable != null)
            {
                inRange.Remove(interactable);
            }
        }

        private void UpdateNearest()
        {
            Interactable nearest = null;
            float best = float.MaxValue;

            for (int i = inRange.Count - 1; i >= 0; i--)
            {
                Interactable candidate = inRange[i];
                if (candidate == null || !candidate.isActiveAndEnabled)
                {
                    inRange.RemoveAt(i);
                    continue;
                }

                float sqr = ((Vector2)candidate.transform.position - body.position).sqrMagnitude;
                if (sqr < best)
                {
                    best = sqr;
                    nearest = candidate;
                }
            }

            if (nearest == current)
            {
                return;
            }

            current = nearest;
            if (hud != null)
            {
                if (current != null)
                {
                    hud.ShowPrompt(current.Prompt);
                }
                else
                {
                    hud.HidePrompt();
                }
            }
        }

        private void OnInteract()
        {
            if (current != null)
            {
                current.Interact();
                // The prompt text may have changed (e.g. the well is now usable, or the pickup is gone).
                current = null;
                UpdateNearest();
            }
        }
    }
}
