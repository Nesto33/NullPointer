using UnityEngine;
using UnityEngine.UIElements;

namespace RiverDeutsch.UI.Shared
{
    /// <summary>Procedurally draws the vaporwave grid (cyan/magenta lines every cellSize px) on resize.</summary>
    public class RetroGridBackground : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private string targetElementName = "retro-grid";
        [SerializeField] private int cellSize = 40;

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
            var mustard = new Color(232f / 255f, 163f / 255f, 61f / 255f, 0.15f);
            var burntOrange = new Color(193f / 255f, 68f / 255f, 14f / 255f, 0.15f);
            var clear = new Color(0f, 0f, 0f, 0f);

            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                bool horizontalLine = y % cellSize == 0;
                for (int x = 0; x < w; x++)
                {
                    bool verticalLine = x % cellSize == 0;
                    pixels[y * w + x] = verticalLine ? mustard : (horizontalLine ? burntOrange : clear);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
            target.style.backgroundImage = new StyleBackground(texture);
        }
    }
}
