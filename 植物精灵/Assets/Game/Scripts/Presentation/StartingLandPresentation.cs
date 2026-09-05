using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlantSpirit.GGJ
{
    public sealed class StartingLandBackdrop : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Camera sceneCamera;

        private void Awake()
        {
            Sprite sprite = ArtResources2D.LoadSprite("Environment/StartingLand/starting_land_background");
            if (sprite == null) { Destroy(gameObject); return; }
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = -100;
            float scale = 13.4f / Mathf.Max(.001f, sprite.bounds.size.y);
            transform.localScale = Vector3.one * scale;
            sceneCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (sceneCamera == null) sceneCamera = Camera.main;
            if (sceneCamera != null) transform.position = new Vector3(sceneCamera.transform.position.x, .15f, 0f);
        }
    }

    public static class StartingLandPresentation
    {
        public static bool IsStartingLand => SceneManager.GetActiveScene().name == "Level01";

        public static void Build(Transform parent)
        {
            if (!IsStartingLand || parent.Find("StartingLandArt") != null) return;
            Transform root = new GameObject("StartingLandArt").transform;
            root.SetParent(parent, false);
            new GameObject("StartingLandBackdrop").AddComponent<StartingLandBackdrop>().transform.SetParent(root, false);

            Add(root, "StartRoots", 70, new Vector2(-14.6f, -3.7f), 1.45f, -2);
            Add(root, "StartLedge", 1, new Vector2(-10.5f, -3.7f), 1.15f, 1);
            Add(root, "UpperRuin", 29, new Vector2(-7.8f, -3.7f), 3.15f, -8, new Color(.72f, .82f, .67f, .75f));
            Add(root, "UpperPlatformEdge", 40, new Vector2(-6.2f, -1.85f), 1.05f, 2);
            Add(root, "MiddleArch", 42, new Vector2(1.4f, -3.7f), 2.25f, -7, new Color(.7f, .82f, .66f, .72f));
            Add(root, "TreasureSanctuary", 60, new Vector2(16.8f, -3.7f), 3.5f, -6, new Color(.77f, .9f, .69f, .82f));
            Add(root, "WaterBasin", 53, new Vector2(23.4f, -3.7f), 2.15f, 1);
            Add(root, "WaterEdge", 13, new Vector2(21.2f, -3.7f), 1.08f, 2);
        }

        public static Sprite Tile(int index) => ArtResources2D.LoadSprite("Environment/StartingLand/environment_" + index.ToString("D3"));

        private static void Add(Transform parent, string name, int tile, Vector2 bottomCenter, float height, int order)
        {
            Add(parent, name, tile, bottomCenter, height, order, Color.white);
        }

        private static void Add(Transform parent, string name, int tile, Vector2 bottomCenter, float height, int order, Color tint)
        {
            ArtResources2D.CreateWorldSprite(parent, name, Tile(tile), bottomCenter, height, order, tint);
        }
    }
}
