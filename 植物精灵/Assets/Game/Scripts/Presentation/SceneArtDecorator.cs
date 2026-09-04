using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class LevelArtDecorator : MonoBehaviour
    {
        public static LevelArtDecorator Ensure()
        {
            LevelArtDecorator existing = FindObjectOfType<LevelArtDecorator>();
            if (existing != null) return existing;
            return new GameObject("LevelArt").AddComponent<LevelArtDecorator>();
        }

        private void Awake()
        {
            if (Camera.main != null) Camera.main.backgroundColor = new Color(.035f, .055f, .045f);
            BuildBackground();
            BuildPlatforms();
            BuildProps();
        }

        private void BuildBackground()
        {
            int[] walls = { 43, 44, 45, 46, 57, 58, 65 };
            for (int i = 0; i < 13; i++)
            {
                Sprite sprite = Ruin(walls[i % walls.Length]);
                SpriteRenderer renderer = AddBottom("BackgroundWall" + i, sprite, new Vector2(-18f + i * 4.5f, -3.72f), 3.15f, -20);
                if (renderer != null) renderer.color = new Color(.34f, .39f, .33f, .32f);
            }

            AddBottom("BackColumnA", Ruin(51), new Vector2(-12f, -3.7f), 3.4f, -12, new Color(.52f, .58f, .46f, .6f));
            AddBottom("BackColumnB", Ruin(54), new Vector2(2f, -3.7f), 3.1f, -12, new Color(.48f, .54f, .43f, .55f));
            AddBottom("BackArch", Ruin(53), new Vector2(11f, -3.7f), 3.5f, -12, new Color(.46f, .52f, .41f, .55f));
            AddBottom("BackColumnC", Ruin(52), new Vector2(22f, -3.7f), 3.35f, -12, new Color(.5f, .56f, .44f, .58f));
        }

        private void BuildPlatforms()
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground != null)
            {
                ArtResources2D.HidePlaceholder(ground);
                Collider2D collider = ground.GetComponent<Collider2D>();
                Sprite tile = Ruin(1);
                if (collider != null && tile != null)
                {
                    float height = 1.15f;
                    float scale = height / tile.bounds.size.y;
                    float width = tile.bounds.size.x * scale;
                    int count = Mathf.CeilToInt(collider.bounds.size.x / width) + 1;
                    float start = collider.bounds.min.x + width * .5f;
                    for (int i = 0; i < count; i++) AddBottom("GroundArt" + i, tile, new Vector2(start + i * width, collider.bounds.max.y - height), height, 0);
                }
            }

            DecoratePlatform("TutorialPlatform", 13, .82f);
            DecoratePlatform("CombatPlatform", 14, .78f);
            DecoratePlatform("CombatPlatform02", 13, .82f);
            DecoratePlatform("CombatPlatform03", 82, .88f);
        }

        private void DecoratePlatform(string objectName, int spriteIndex, float height)
        {
            GameObject platform = GameObject.Find(objectName);
            Sprite sprite = Ruin(spriteIndex);
            if (platform == null || sprite == null) return;
            ArtResources2D.HidePlaceholder(platform);
            Collider2D collider = platform.GetComponent<Collider2D>();
            if (collider == null) return;
            SpriteRenderer renderer = AddBottom(objectName + "Art", sprite, new Vector2(collider.bounds.center.x, collider.bounds.max.y - height), height, 1);
            if (renderer != null)
            {
                Vector3 scale = renderer.transform.localScale;
                scale.x = (collider.bounds.size.x + .16f) / Mathf.Max(.001f, sprite.bounds.size.x);
                renderer.transform.localScale = scale;
                Vector3 position = renderer.transform.position;
                position.x = collider.bounds.center.x - sprite.bounds.center.x * scale.x;
                renderer.transform.position = position;
            }
        }

        private void BuildProps()
        {
            AddBottom("FlowerA", Ruin(6), new Vector2(-15.5f, -3.68f), .68f, 2);
            AddBottom("FlowerB", Ruin(8), new Vector2(-10.6f, -3.68f), .58f, 2);
            AddBottom("GrassA", Ruin(10), new Vector2(-4.8f, -3.68f), .48f, 2);
            AddBottom("MushroomsA", Ruin(17), new Vector2(.2f, -3.68f), .62f, 2);
            AddBottom("RocksA", Ruin(18), new Vector2(5.8f, -3.68f), .55f, 2);
            AddBottom("FlowerC", Ruin(12), new Vector2(9.3f, -3.68f), .72f, 2);
            AddBottom("RocksB", Ruin(16), new Vector2(17.2f, -3.68f), .65f, 2);
            AddBottom("FlowerD", Ruin(20), new Vector2(25.2f, -3.68f), .64f, 2);
            AddBottom("RootsA", Ruin(37), new Vector2(-1.8f, -3.68f), 1.25f, -2, new Color(.75f, .8f, .66f, .7f));
            AddBottom("RuinMarker", Ruin(63), new Vector2(16.2f, -3.7f), 1.45f, -1);
        }

        private SpriteRenderer AddBottom(string objectName, Sprite sprite, Vector2 bottomCenter, float height, int order)
        {
            return AddBottom(objectName, sprite, bottomCenter, height, order, Color.white);
        }

        private SpriteRenderer AddBottom(string objectName, Sprite sprite, Vector2 bottomCenter, float height, int order, Color tint)
        {
            return ArtResources2D.CreateWorldSprite(transform, objectName, sprite, bottomCenter, height, order, tint);
        }

        private static Sprite Ruin(int index) => ArtResources2D.LoadSprite("Environment/Ruins/ruins_" + index.ToString("D3"));
    }

    public static class MenuArtDecorator
    {
        public static void Ensure()
        {
            if (GameObject.Find("MenuArt") != null) return;
            Sprite player = ArtResources2D.LoadSequence("Player/AttackA").Length > 0 ? ArtResources2D.LoadSequence("Player/AttackA")[0] : null;
            if (player == null) return;
            if (Camera.main != null) Camera.main.backgroundColor = new Color(.035f, .055f, .045f);
            Transform root = new GameObject("MenuArt").transform;
            Sprite wall = ArtResources2D.LoadSprite("Environment/Ruins/ruins_043");
            for (int i = 0; i < 6; i++)
            {
                SpriteRenderer renderer = ArtResources2D.CreateWorldSprite(root, "MenuWall" + i, wall, new Vector2(-5f + i * 4f, -5.35f), 3.5f, -10, new Color(.45f, .5f, .4f, .55f));
                if (renderer != null && i % 2 == 1) renderer.flipX = true;
            }
            ArtResources2D.CreateWorldSprite(root, "MenuPlayer", player, new Vector2(10.5f, -4.4f), 3.5f, -1, Color.white);
            ArtResources2D.CreateWorldSprite(root, "MenuArch", ArtResources2D.LoadSprite("Environment/Ruins/ruins_053"), new Vector2(-1.8f, -4.5f), 4f, -3, new Color(.72f, .76f, .62f, .8f));
            ArtResources2D.CreateWorldSprite(root, "MenuFlower", ArtResources2D.LoadSprite("Environment/Ruins/ruins_012"), new Vector2(5.5f, -4.45f), 1.1f, -1, Color.white);
        }
    }
}
