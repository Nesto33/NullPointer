using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace RiverDeutsch.UI.Shared
{
    /// <summary>
    /// Plays an old-CRT power-on once when the screen loads: a held black frame with
    /// crackling static, then two masks shrink away from a bright center seam to
    /// reveal the actual UI, like a tube warming up.
    /// </summary>
    public class TvPowerOnEffect : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private string topMaskElementName = "tv-mask-top";
        [SerializeField] private string bottomMaskElementName = "tv-mask-bottom";
        [SerializeField] private string noiseLayerElementName = "noise-layer";

        [SerializeField] private float holdDuration = 0.18f;
        [SerializeField] private float revealDuration = 0.45f;
        [SerializeField] private float crackleTailDuration = 0.3f;
        [SerializeField] private float crackleOpacity = 0.75f;

        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            VisualElement top = root.Q<VisualElement>(topMaskElementName);
            VisualElement bottom = root.Q<VisualElement>(bottomMaskElementName);
            VisualElement noise = root.Q<VisualElement>(noiseLayerElementName);

            if (top == null || bottom == null) return;

            StartCoroutine(PlayPowerOn(top, bottom, noise));
        }

        private IEnumerator PlayPowerOn(VisualElement top, VisualElement bottom, VisualElement noise)
        {
            top.style.height = Length.Percent(50);
            bottom.style.height = Length.Percent(50);
            top.pickingMode = PickingMode.Position;
            bottom.pickingMode = PickingMode.Position;
            if (noise != null) noise.style.opacity = crackleOpacity;

            // Hold on black for a beat, tube "warming up".
            float elapsed = 0f;
            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Masks shrink away from the center seam, ease-out.
            elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
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
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / crackleTailDuration);
                    noise.style.opacity = Mathf.Lerp(crackleOpacity, 0.12f, t);
                    yield return null;
                }
                noise.style.opacity = StyleKeyword.Null;
            }
        }
    }
}
