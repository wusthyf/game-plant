using UnityEngine;

namespace PlantSpirit.GGJ
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlaceholderVisual : MonoBehaviour
    {
        private static Sprite sprite;
        public void Configure(Color color, Vector2 size, int order = 0)
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (sprite == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white); texture.Apply();
                sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
            }
            renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }
    }
}
