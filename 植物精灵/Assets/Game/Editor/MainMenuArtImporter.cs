using UnityEditor;
using UnityEngine;

namespace PlantSpirit.GGJ.Editor
{
    public sealed class MainMenuArtImporter : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/Game/Art/Resources/PlantSpirit/UI/MainMenu/";

        private void OnPreprocessTexture()
        {
            string normalizedPath = assetPath.Replace('\\', '/');
            if (!normalizedPath.StartsWith(ArtFolder, System.StringComparison.Ordinal)) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
        }

        [MenuItem("Plant Spirit/Refresh Main Menu Art")]
        private static void RefreshMainMenuArt()
        {
            AssetDatabase.ImportAsset(ArtFolder + "main_menu_background.png", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ArtFolder + "main_menu_title.png", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ArtFolder + "main_menu_button.png", ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
        }
    }
}
