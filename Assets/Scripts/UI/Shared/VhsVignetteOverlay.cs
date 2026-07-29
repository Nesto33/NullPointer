using UnityEngine;
using UnityEngine.UIElements;

namespace RiverDeutsch.UI.Shared
{
    /// <summary>Darkens the screen's corners/edges once per resize, like watching through a CRT/tape lens.</summary>
    public class VhsVignetteOverlay : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private string targetElementName = "vignette-layer";
        [SerializeField, Range(0f, 1f)] private float innerRadius = 0.45f;
        [SerializeField, Range(0.5f, 2f)] private float outerRadius = 1.15f;
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.55f;

        private VisualElement target;

        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            target = root.Q<VisualElement>(targetElementName);
            if (target == null) return;

            target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnDisable()
        {
            if (target != null) target.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            int w = Mathf.Max(1, Mathf.RoundToInt(evt.newRect.width));
            int h = Mathf.Max(1, Mathf.RoundToInt(evt.newRect.height));

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[w * h];

            float cx = w / 2f;
            float cy = h / 2f;

            for (int y = 0; y < h; y++)
            {
                float dy = (y - cy) / cy;
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / cx;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((dist - innerRadius) / (outerRadius - innerRadius)) * maxAlpha;
                    pixels[y * w + x] = new Color(0f, 0f, 0f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
            target.style.backgroundImage = new StyleBackground(texture);
        }
    }
}
