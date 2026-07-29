using UnityEngine;
using UnityEngine.UIElements;

namespace RiverDeutsch.UI.Shared
{
    /// <summary>Jitters three overlapping same-text labels for an RGB-split glitch look.</summary>
    public class GlitchTitleAnimator : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private string cyanElementName = "title-cyan";
        [SerializeField] private string magentaElementName = "title-magenta";
        [SerializeField] private string whiteElementName = "title-white";

        private VisualElement cyan;
        private VisualElement magenta;
        private VisualElement white;
        private float lastUpdate;

        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            cyan = root.Q<VisualElement>(cyanElementName);
            magenta = root.Q<VisualElement>(magentaElementName);
            white = root.Q<VisualElement>(whiteElementName);
        }

        private void Update()
        {
            if (cyan == null || magenta == null || white == null) return;
            if (Time.unscaledTime - lastUpdate < 0.05f) return;
            lastUpdate = Time.unscaledTime;

            if (Random.value > 0.9f)
            {
                float shiftX = Random.Range(0, 10) - 5;
                float shiftY = Random.Range(0, 5) - 2;
                cyan.style.translate = new Translate(-shiftX * 2, 0);
                magenta.style.translate = new Translate(shiftX * 2, 0);
                white.style.translate = new Translate(0, shiftY);
                white.style.display = Random.value > 0.5f ? DisplayStyle.Flex : DisplayStyle.None;
            }
            else
            {
                cyan.style.translate = new Translate(-2, 0);
                magenta.style.translate = new Translate(2, 0);
                white.style.translate = new Translate(0, 0);
                white.style.display = DisplayStyle.Flex;
            }
        }
    }
}
