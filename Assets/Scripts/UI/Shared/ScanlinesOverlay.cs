using UnityEngine;
using UnityEngine.UIElements;

namespace RiverDeutsch.UI.Shared
{
    /// <summary>Procedurally draws repeating horizontal CRT scanlines once per resize.</summary>
    public class ScanlinesOverlay : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private string targetElementName = "scanlines";
        [SerializeField] private int lineSpacing = 4;
        [SerializeField, Range(0f, 1f)] private float lineAlpha = 0.35f;

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

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var dark = new Color(0f, 0f, 0f, lineAlpha);
            var clear = new Color(0f, 0f, 0f, 0f);

            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                Color rowColor = y % lineSpacing == 0 ? dark : clear;
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = rowColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
            target.style.backgroundImage = new StyleBackground(texture);
        }
    }
}
