using UnityEngine;

namespace PlantSpirit.GGJ
{
    public static class WorldArtPresentation2D
    {
        public static void AttachProjectile(GameObject projectile, Vector2 velocity)
        {
            if (projectile.transform.Find("ProjectileArt") != null) return;
            Sprite[] frames = ArtResources2D.LoadSequence("Vfx/Spore");
            if (frames.Length == 0) return;
            float height = projectile.layer == LayerMask.NameToLayer("EnemyProjectile") ? .42f : .32f;
            SpriteSequence2D visual = SpriteSequence2D.Create(projectile.transform, "ProjectileArt", height, 5, false);
            visual.PlayLoop(frames, 12f);
            visual.SetFacingLeft(velocity.x < 0f);
        }

        public static void SpawnBurst(Vector3 position, float height = .7f)
        {
            Sprite[] frames = ArtResources2D.LoadSequence("Vfx/Burst");
            if (frames.Length == 0) return;
            GameObject burst = new GameObject("ImpactBurst");
            burst.transform.position = position;
            SpriteSequence2D visual = SpriteSequence2D.Create(burst.transform, "BurstArt", height, 7, false, false);
            visual.PlayOnce(frames, 16f, () => Object.Destroy(burst));
        }

        public static void AttachPickup(GameObject pickup, GraftDefinition definition)
        {
            if (definition == null || pickup.transform.Find("PickupArt") != null) return;
            string path = definition.Slot == GraftSlot.Root
                ? "Environment/Ruins/ruins_037"
                : definition.Slot == GraftSlot.Stem
                    ? "Environment/Ruins/ruins_039"
                    : "Environment/Ruins/ruins_017";
            Sprite sprite = ArtResources2D.LoadSprite(path);
            if (sprite == null) return;
            SpriteSequence2D visual = SpriteSequence2D.Create(pickup.transform, "PickupArt", .72f, 5, false);
            visual.Show(sprite);
        }

        public static void AttachPortal(GameObject portal)
        {
            if (portal.transform.Find("PortalArch") != null) return;
            Sprite arch = ArtResources2D.LoadSprite("Environment/Ruins/ruins_053");
            Sprite[] burst = ArtResources2D.LoadSequence("Vfx/Burst");
            if (burst.Length > 0)
            {
                SpriteSequence2D glow = SpriteSequence2D.Create(portal.transform, "PortalGlow", 1.25f, 1, false, false);
                glow.PlayLoop(burst, 9f);
                glow.SetTint(new Color(.55f, 1f, .72f, .82f));
            }
            if (arch != null)
            {
                SpriteSequence2D frame = SpriteSequence2D.Create(portal.transform, "PortalArch", 2.7f, 2, true);
                frame.Show(arch);
            }
        }

        public static void AttachPoisonZone(GameObject zone)
        {
            if (zone.transform.Find("PoisonArt") != null) return;
            Sprite mushroom = ArtResources2D.LoadSprite("Environment/Ruins/ruins_017");
            if (mushroom == null) return;
            SpriteSequence2D visual = SpriteSequence2D.Create(zone.transform, "PoisonArt", .75f, 2, false, false);
            visual.Show(mushroom);
            visual.SetTint(new Color(.82f, .62f, 1f, .9f));
        }
    }
}
