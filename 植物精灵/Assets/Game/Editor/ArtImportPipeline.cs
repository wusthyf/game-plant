using UnityEditor;
using UnityEngine;

namespace PlantSpirit.GGJ.Editor
{
    public static class ArtImportPipeline
    {
        private const string ArtRoot = "Assets/Game/Art/Resources/PlantSpirit";

        [MenuItem("Plant Spirit/Import Supplied Art")]
        public static void ImportSuppliedArt()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot });
            int imported = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 256f;
                importer.spritePivot = new Vector2(.5f, .5f);
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.isReadable = false;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
                imported++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantSpiritArt] Imported " + imported + " supplied textures.");
        }
    }
}
