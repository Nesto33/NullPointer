using UnityEngine;
using UnityEngine.UIElements;

namespace RiverDeutsch.UI.Shared
{
    /// <summary>Old-CRT static noise, redrawn ~20fps onto a VisualElement's background texture.</summary>
    public class StaticNoiseOverlay : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private string targetElementName = "noise-layer";
        [SerializeField] private int dotsPerFrame = 500;

        private VisualElement target;
        private Texture2D texture;
        private Color32[] clearBuffer;
        private float lastUpdate;

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
            if (texture != null && texture.width == w && texture.height == h) return;

            texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            clearBuffer = new Color32[w * h];
            target.style.backgroundImage = new StyleBackground(texture);
        }

        private void Update()
        {
            if (texture == null) return;
            if (Time.unscaledTime - lastUpdate < 0.05f) return;
            lastUpdate = Time.unscaledTime;
            DrawNoise();
        }

        private void DrawNoise()
        {
            texture.SetPixels32(clearBuffer);

            int w = texture.width;
            int h = texture.height;
            for (int i = 0; i < dotsPerFrame; i++)
            {
                int x = Random.Range(0, w);
                int y = Random.Range(0, h);
                texture.SetPixel(x, y, Color.white);
            }
            texture.Apply(false);
        }
    }
}
