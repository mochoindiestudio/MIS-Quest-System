using System.Collections;
using TMPro;
using UnityEngine;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// A transient message near the top of the screen. <see cref="Show"/> fades it in, holds it, then
    /// fades it out -- all on unscaled time, so it still animates while the pause menu freezes the game.
    /// </summary>
    public sealed class ToastView : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup group;

        [SerializeField]
        private TMP_Text label;

        [SerializeField]
        private float fadeSeconds = 0.25f;

        private Coroutine running;

        private void Awake()
        {
            if (group != null)
            {
                group.alpha = 0f;
            }
        }

        /// <summary>Shows <paramref name="message"/> for <paramref name="holdSeconds"/> (plus fades).</summary>
        public void Show(string message, float holdSeconds)
        {
            if (label != null)
            {
                label.text = message;
            }

            if (running != null)
            {
                StopCoroutine(running);
            }

            running = StartCoroutine(Run(holdSeconds));
        }

        private IEnumerator Run(float hold)
        {
            yield return Fade(1f);

            float t = 0f;
            while (t < hold)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return Fade(0f);
            running = null;
        }

        private IEnumerator Fade(float target)
        {
            if (group == null)
            {
                yield break;
            }

            float start = group.alpha;
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, t / fadeSeconds);
                yield return null;
            }

            group.alpha = target;
        }
    }
}
