using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PlantSpirit.GGJ
{
    public sealed class BossApproachPrompt : MonoBehaviour
    {
        private const string PromptText = "前方就是腐化古树的巢穴.....";
        private Text text;
        private RectTransform textRect;
        private Canvas canvas;
        private bool shown;

        private void Awake() => CreateUi();

        public void Show()
        {
            if (shown) return;
            shown = true;
            StartCoroutine(Play());
        }

        private void CreateUi()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>().enabled = false;
            GameObject label = new GameObject("NestWarning", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(transform, false);
            textRect = label.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(.5f, 0f);
            textRect.anchorMax = new Vector2(.5f, 0f);
            textRect.pivot = new Vector2(.5f, 0f);
            textRect.anchoredPosition = new Vector2(0f, 165f);
            textRect.sizeDelta = new Vector2(1040f, 88f);
            text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = PromptText;
            text.color = new Color(.91f, .79f, 1f, 0f);
        }

        private IEnumerator Play()
        {
            yield return Fade(0f, 1f, .35f);
            SpawnDrips();
            yield return new WaitForSeconds(2.4f);
            yield return Fade(1f, 0f, .45f);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                Color color = text.color;
                color.a = Mathf.Lerp(from, to, elapsed / duration);
                text.color = color;
                yield return null;
            }
            Color finalColor = text.color;
            finalColor.a = to;
            text.color = finalColor;
        }

        private void SpawnDrips()
        {
            int count = PromptText.Length + 5;
            for (int i = 0; i < count; i++)
            {
                GameObject drop = new GameObject("PurpleDrip", typeof(RectTransform), typeof(Image));
                drop.transform.SetParent(transform, false);
                RectTransform rect = drop.GetComponent<RectTransform>();
                float position = Mathf.Lerp(-330f, 330f, (i + .5f) / count) + Random.Range(-8f, 8f);
                rect.anchorMin = new Vector2(.5f, 0f);
                rect.anchorMax = new Vector2(.5f, 0f);
                rect.pivot = new Vector2(.5f, 1f);
                rect.anchoredPosition = new Vector2(position, 160f + Random.Range(-10f, 10f));
                Image image = drop.GetComponent<Image>();
                image.color = Color.Lerp(new Color(.42f, .16f, .5f, .94f), new Color(.7f, .35f, .91f, .78f), Random.value);
                StartCoroutine(AnimateDrip(rect, image));
            }
        }

        private IEnumerator AnimateDrip(RectTransform rect, Image image)
        {
            float stretch = Random.Range(.18f, .3f);
            float fall = Random.Range(.35f, .6f);
            float elapsed = 0f;
            while (elapsed < stretch)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / stretch;
                rect.sizeDelta = new Vector2(4f + t * 3f, 6f + t * 22f);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < fall)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fall;
                rect.anchoredPosition += Vector2.down * (140f * Time.unscaledDeltaTime);
                rect.sizeDelta = Vector2.Lerp(new Vector2(7f, 28f), new Vector2(12f, 7f), t);
                Color color = image.color;
                color.a = Mathf.Lerp(.9f, 0f, t);
                image.color = color;
                yield return null;
            }
            Destroy(rect.gameObject);
        }
    }
}
