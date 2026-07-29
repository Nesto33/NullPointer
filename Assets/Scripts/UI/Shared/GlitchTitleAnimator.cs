using UnityEngine;
using UnityEngine.UIElements;

namespace RiverDeutsch.UI.Shared
{
    /// <summary>Jitters three overlapping same-text labels for an RGB-split glitch look.</summary>
    public class GlitchTitleAnimator : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private string accentAElementName = "title-accent-a";
        [SerializeField] private string accentBElementName = "title-accent-b";
        [SerializeField] private string baseElementName = "title-base";

        private VisualElement accentA;
        private VisualElement accentB;
        private VisualElement baseLayer;
        private float lastUpdate;

        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            accentA = root.Q<VisualElement>(accentAElementName);
            accentB = root.Q<VisualElement>(accentBElementName);
            baseLayer = root.Q<VisualElement>(baseElementName);
        }

        private void Update()
        {
            if (accentA == null || accentB == null || baseLayer == null) return;
            if (Time.unscaledTime - lastUpdate < 0.05f) return;
            lastUpdate = Time.unscaledTime;

            if (Random.value > 0.9f)
            {
                float shiftX = Random.Range(0, 10) - 5;
                float shiftY = Random.Range(0, 5) - 2;
                accentA.style.translate = new Translate(-shiftX * 2, 0);
                accentB.style.translate = new Translate(shiftX * 2, 0);
                baseLayer.style.translate = new Translate(0, shiftY);
                baseLayer.style.display = Random.value > 0.5f ? DisplayStyle.Flex : DisplayStyle.None;
            }
            else
            {
                accentA.style.translate = new Translate(-2, 0);
                accentB.style.translate = new Translate(2, 0);
                baseLayer.style.translate = new Translate(0, 0);
                baseLayer.style.display = DisplayStyle.Flex;
            }
        }
    }
}
