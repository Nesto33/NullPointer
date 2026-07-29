using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace RiverDeutsch.UI.Shared
{
    /// <summary>Occasional horizontal "tape mistracking" wobble, the iconic VHS artifact.</summary>
    public class VhsTrackingGlitch : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private string screenElementName = "screen-root";
        [SerializeField] private string noiseLayerElementName = "noise-layer";

        [SerializeField] private float minIntervalSeconds = 2.5f;
        [SerializeField] private float maxIntervalSeconds = 7f;
        [SerializeField] private float glitchDuration = 0.12f;
        [SerializeField] private float maxOffsetPixels = 14f;

        private VisualElement screen;
        private VisualElement noise;

        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            screen = root.Q<VisualElement>(screenElementName);
            noise = root.Q<VisualElement>(noiseLayerElementName);
            if (screen == null) return;

            StartCoroutine(GlitchLoop());
        }

        private IEnumerator GlitchLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(minIntervalSeconds, maxIntervalSeconds));
                yield return StartCoroutine(PlayGlitch());
            }
        }

        private IEnumerator PlayGlitch()
        {
            float offset = Random.Range(-maxOffsetPixels, maxOffsetPixels);
            if (noise != null) noise.style.opacity = 0.5f;

            float elapsed = 0f;
            while (elapsed < glitchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / glitchDuration;
                // Snap back and forth a couple of times rather than a smooth slide.
                float wobble = Mathf.Sin(t * Mathf.PI * 3f) * (1f - t);
                screen.style.translate = new Translate(offset * wobble, 0);
                yield return null;
            }

            screen.style.translate = new Translate(0, 0);
            if (noise != null) noise.style.opacity = StyleKeyword.Null;
        }
    }
}
