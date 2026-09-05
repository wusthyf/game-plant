using UnityEngine;
using UnityEngine.UI;

namespace PlantSpirit.GGJ
{
    public sealed class GraftPanelPresentation : MonoBehaviour
    {
        private const string ResourceRoot = "PlantSpirit/UI/Inventory/";
        [SerializeField] private Image backdrop;
        [SerializeField] private Sprite emptyState;
        [SerializeField] private Sprite acquiredState;
        [SerializeField] private Sprite rootEquippedState;
        [SerializeField] private Sprite fullyEquippedState;

        public void Configure(Image target, Sprite empty, Sprite acquired, Sprite rootEquipped, Sprite fullyEquipped)
        {
            backdrop = target;
            emptyState = empty;
            acquiredState = acquired;
            rootEquippedState = rootEquipped;
            fullyEquippedState = fullyEquipped;
        }

        public static GraftPanelPresentation Ensure(GameObject panel, Button root, Button stem, Button flower, Button close)
        {
            if (panel == null || root == null || stem == null || flower == null || close == null) return null;

            Sprite empty = Resources.Load<Sprite>(ResourceRoot + "inventory-b-state-empty");
            Sprite acquired = Resources.Load<Sprite>(ResourceRoot + "inventory-b-state-acquired");
            Sprite rootEquipped = Resources.Load<Sprite>(ResourceRoot + "inventory-b-state-selected-iron-root");
            Sprite fullyEquipped = Resources.Load<Sprite>(ResourceRoot + "inventory-b-state-equipped-all");
            if (empty == null || acquired == null || rootEquipped == null || fullyEquipped == null) return null;

            Image backdrop = panel.GetComponent<Image>();
            if (backdrop == null) return null;
            backdrop.sprite = empty;
            backdrop.type = Image.Type.Simple;
            backdrop.preserveAspect = true;
            backdrop.color = Color.white;
            backdrop.rectTransform.sizeDelta = new Vector2(1400f, 788f);

            GraftPanelPresentation presentation = panel.GetComponent<GraftPanelPresentation>();
            if (presentation == null) presentation = panel.AddComponent<GraftPanelPresentation>();
            presentation.Configure(backdrop, empty, acquired, rootEquipped, fullyEquipped);

            ConfigureHotspot(root, new Vector2(-206f, -247f), new Vector2(210f, 72f));
            ConfigureHotspot(stem, new Vector2(0f, -247f), new Vector2(210f, 72f));
            ConfigureHotspot(flower, new Vector2(206f, -247f), new Vector2(210f, 72f));
            ConfigureHotspot(close, new Vector2(0f, -352f), new Vector2(320f, 76f));
            return presentation;
        }

        public void Refresh(GameSession session)
        {
            if (backdrop == null || session == null) return;

            bool rootEquipped = session.Get(GraftSlot.Root) != null;
            bool stemEquipped = session.Get(GraftSlot.Stem) != null;
            bool flowerEquipped = session.Get(GraftSlot.Flower) != null;
            Sprite next = session.Inventory.Count == 0
                ? emptyState
                : rootEquipped && stemEquipped && flowerEquipped
                    ? fullyEquippedState
                    : rootEquipped
                        ? rootEquippedState
                        : acquiredState;

            if (next != null) backdrop.sprite = next;
        }

        private static void ConfigureHotspot(Button button, Vector2 position, Vector2 size)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = button.GetComponent<Image>();
            image.color = Color.clear;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.enabled = false;
        }
    }
}
