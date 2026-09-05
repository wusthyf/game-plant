using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlantSpirit.GGJ
{
    /// <summary>
    /// Applies the generated pixel-storybook art without replacing the scene's
    /// existing menu behaviour. The scene remains functional if the art is absent.
    /// </summary>
    public static class MainMenuVisualTheme
    {
        private const string MarkerName = "GeneratedMainMenuTheme";
        private const string ResourceRoot = "PlantSpirit/UI/MainMenu/";

        public static bool Ensure(Button start, Button controls, Button audio, Button quit)
        {
            if (GameObject.Find(MarkerName) != null) return true;

            Sprite background = Resources.Load<Sprite>(ResourceRoot + "main_menu_background");
            Sprite title = Resources.Load<Sprite>(ResourceRoot + "main_menu_title");
            Sprite buttonFrame = Resources.Load<Sprite>(ResourceRoot + "main_menu_button");
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (background == null || title == null || buttonFrame == null || canvas == null) return false;

            DisableOriginalBackdropAndTitle(canvas);

            RectTransform artRoot = CreateRect(MarkerName, canvas.transform);
            Stretch(artRoot);
            artRoot.SetAsFirstSibling();

            Image backgroundImage = CreateImage("PixelForestBackground", artRoot, background);
            RectTransform backgroundRect = backgroundImage.rectTransform;
            backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(.5f, .5f);
            backgroundRect.anchoredPosition = Vector2.zero;
            AspectRatioFitter fitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = background.rect.width / background.rect.height;

            Image titleImage = CreateImage("PlantSpiritTitle", artRoot, title);
            titleImage.preserveAspect = true;
            Place(titleImage.rectTransform, new Vector2(.755f, .79f), new Vector2(760f, 330f));

            RectTransform buttonRoot = CreateRect("MainMenuButtons", canvas.transform);
            Stretch(buttonRoot);
            buttonRoot.SetSiblingIndex(Mathf.Min(1, canvas.transform.childCount - 1));

            StyleButton(start, buttonFrame, buttonRoot, new Vector2(.755f, .535f), true);
            StyleButton(controls, buttonFrame, buttonRoot, new Vector2(.755f, .405f), false);
            StyleButton(audio, buttonFrame, buttonRoot, new Vector2(.755f, .275f), false);
            StyleButton(quit, buttonFrame, buttonRoot, new Vector2(.755f, .145f), false);

            if (EventSystem.current != null && start != null)
            {
                EventSystem.current.SetSelectedGameObject(start.gameObject);
            }

            return true;
        }

        private static void DisableOriginalBackdropAndTitle(Canvas canvas)
        {
            foreach (Transform child in canvas.transform)
            {
                if (child.name == "Background") child.gameObject.SetActive(false);
            }

            foreach (Text text in canvas.GetComponentsInChildren<Text>(true))
            {
                if (text.text == "植物精灵") text.enabled = false;
            }
        }

        private static void StyleButton(Button button, Sprite frame, Transform parent, Vector2 anchor, bool selected)
        {
            if (button == null) return;

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, anchor, new Vector2(650f, 118f));

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = frame;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = selected ? new Color32(245, 255, 218, 255) : Color.white;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = selected ? new Color32(245, 255, 218, 255) : Color.white;
            colors.highlightedColor = new Color32(220, 255, 166, 255);
            colors.pressedColor = new Color32(150, 196, 106, 255);
            colors.selectedColor = new Color32(220, 255, 166, 255);
            colors.disabledColor = new Color32(105, 115, 92, 180);
            colors.colorMultiplier = 1.08f;
            colors.fadeDuration = .08f;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colors;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                RectTransform labelRect = label.rectTransform;
                Stretch(labelRect);
                labelRect.offsetMin = new Vector2(48f, 15f);
                labelRect.offsetMax = new Vector2(-48f, -10f);
                label.alignment = TextAnchor.MiddleCenter;
                label.color = new Color32(255, 239, 190, 255);
                label.fontStyle = FontStyle.Bold;
                label.fontSize = 40;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 26;
                label.resizeTextMaxSize = 42;

                Shadow shadow = label.GetComponent<Shadow>();
                if (shadow == null) shadow = label.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, .05f, .015f, .9f);
                shadow.effectDistance = new Vector2(3f, -3f);
                shadow.useGraphicAlpha = true;
            }

            if (button.GetComponent<MainMenuButtonMotion>() == null)
            {
                button.gameObject.AddComponent<MainMenuButtonMotion>();
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject instance = new GameObject(name, typeof(RectTransform));
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }

    public sealed class MainMenuButtonMotion : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        private RectTransform cachedRect;
        private bool highlighted;
        private bool pressed;

        private void Awake() => cachedRect = transform as RectTransform;

        private void OnEnable()
        {
            if (cachedRect == null) cachedRect = transform as RectTransform;
            if (cachedRect != null) cachedRect.localScale = Vector3.one;
        }

        private void Update()
        {
            if (cachedRect == null) return;
            float scale = pressed ? .975f : highlighted ? 1.035f : 1f;
            cachedRect.localScale = Vector3.Lerp(
                cachedRect.localScale,
                Vector3.one * scale,
                1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        }

        public void OnPointerEnter(PointerEventData eventData) => highlighted = true;
        public void OnPointerExit(PointerEventData eventData) { highlighted = false; pressed = false; }
        public void OnPointerDown(PointerEventData eventData) => pressed = true;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
        public void OnSelect(BaseEventData eventData) => highlighted = true;
        public void OnDeselect(BaseEventData eventData) => highlighted = false;
    }
}
