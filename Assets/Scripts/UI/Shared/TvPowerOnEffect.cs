using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace RiverDeutsch.UI.Shared
{
    /// <summary>
    /// Plays an old-CRT power-on once when the screen loads: a few hesitant flickers,
    /// a held black frame with crackling static, then two masks shrink away from a
    /// bright center seam to reveal the actual UI, like a tube warming up.
    /// </summary>
    public class TvPowerOnEffect : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private string topMaskElementName = "tv-mask-top";
        [SerializeField] private string bottomMaskElementName = "tv-mask-bottom";
        [SerializeField] private string noiseLayerElementName = "noise-layer";

        [Header("Timings (seconds)")]
        [SerializeField] private float[] flickerOnDurations = { 0.10f, 0.08f, 0.06f };
        [SerializeField] private float[] flickerOffDurations = { 0.22f, 0.16f, 0.10f };
        [SerializeField] private float holdDuration = 0.6f;
        [SerializeField] private float revealDuration = 1.4f;
        [SerializeField] private float crackleTailDuration = 0.8f;
        [SerializeField] private float crackleOpacity = 0.75f;
        [SerializeField] private float flickerOpacity = 0.9f;

        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            VisualElement top = root.Q<VisualElement>(topMaskElementName);
            VisualElement bottom = root.Q<VisualElement>(bottomMaskElementName);
            VisualElement noise = root.Q<VisualElement>(noiseLayerElementName);

            if (top == null || bottom == null) return;

            StartCoroutine(PlayPowerOn(top, bottom, noise));
        }

        // The very first frame after entering Play Mode (or loading this scene) often
        // reports a hugely inflated Time.deltaTime — it bundles in load/compile time.
        // Counting that would eat most of the animation in a single step, so every
        // hand-timed loop below skips a frame first and clamps its per-frame delta.
        private const float MaxStepSeconds = 0.05f;

        private IEnumerator PlayPowerOn(VisualElement top, VisualElement bottom, VisualElement noise)
        {
            top.style.height = Length.Percent(50);
            bottom.style.height = Length.Percent(50);
            top.pickingMode = PickingMode.Position;
            bottom.pickingMode = PickingMode.Position;
            if (noise != null) noise.style.opacity = 0f;

            yield return null; // let the inflated first-frame delta pass unused

            // A few hesitant flickers before the tube commits to staying on.
            int flickerCount = Mathf.Min(flickerOnDurations.Length, flickerOffDurations.Length);
            for (int i = 0; i < flickerCount; i++)
            {
                if (noise != null) noise.style.opacity = flickerOpacity;
                yield return WaitFor(flickerOnDurations[i]);

                if (noise != null) noise.style.opacity = 0.05f;
                yield return WaitFor(flickerOffDurations[i]);
            }

            // Hold on black for a beat, tube "warming up".
            if (noise != null) noise.style.opacity = crackleOpacity;
            yield return WaitFor(holdDuration);

            // Masks shrink away from the center seam, ease-out.
            float elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds);
                float t = Mathf.Clamp01(elapsed / revealDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float percent = Mathf.Lerp(50f, 0f, eased);

                top.style.height = Length.Percent(percent);
                bottom.style.height = Length.Percent(percent);

                yield return null;
            }

            top.style.height = Length.Percent(0);
            bottom.style.height = Length.Percent(0);
            top.pickingMode = PickingMode.Ignore;
            bottom.pickingMode = PickingMode.Ignore;

            // Static crackles down to its normal ambient level.
            if (noise != null)
            {
                elapsed = 0f;
                while (elapsed < crackleTailDuration)
                {
                    elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds);
                    float t = Mathf.Clamp01(elapsed / crackleTailDuration);
                    noise.style.opacity = Mathf.Lerp(crackleOpacity, 0.12f, t);
                    yield return null;
                }
                noise.style.opacity = StyleKeyword.Null;
            }
        }

        private static IEnumerator WaitFor(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds);
                yield return null;
            }
        }
    }
}
