using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal sealed class InventoryArtImporter : AssetPostprocessor
{
    private const string ProductionRoot = "/Game/Art/UI/Inventory/Production/";

    private static readonly Dictionary<string, Vector2Int> AtlasGrids = new Dictionary<string, Vector2Int>(StringComparer.OrdinalIgnoreCase)
    {
        ["inventory-b-button-states.png"] = new Vector2Int(1, 5),
        ["inventory-b-slot-states.png"] = new Vector2Int(1, 5),
        ["inventory-b-cell-states.png"] = new Vector2Int(3, 2),
        ["inventory-b-effect-icons-atlas.png"] = new Vector2Int(3, 2),
        ["inventory-b-category-badges-atlas.png"] = new Vector2Int(4, 2),
        ["inventory-b-focus-overlays.png"] = new Vector2Int(3, 1),
        ["inventory-b-items-atlas.png"] = new Vector2Int(3, 3),
        ["inventory-b-notification-banners.png"] = new Vector2Int(1, 3),
        ["inventory-b-equipment-overlays.png"] = new Vector2Int(3, 1),
        ["inventory-b-connectors-atlas.png"] = new Vector2Int(4, 3),
        ["inventory-b-panels-atlas.png"] = new Vector2Int(2, 2),
        ["inventory-b-character-variants.png"] = new Vector2Int(4, 2),
        ["inventory-b-vfx-root.png"] = new Vector2Int(4, 2),
        ["inventory-b-vfx-vine.png"] = new Vector2Int(4, 2),
        ["inventory-b-vfx-flower.png"] = new Vector2Int(4, 2),
    };

    private void OnPreprocessTexture()
    {
        if (assetPath.IndexOf(ProductionRoot, StringComparison.OrdinalIgnoreCase) < 0)
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;

        string fileName = System.IO.Path.GetFileName(assetPath);
        if (!AtlasGrids.TryGetValue(fileName, out Vector2Int grid))
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            return;
        }

        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
#pragma warning disable 618 // Unity 2022 LTS still imports legacy SpriteMetaData correctly.
        importer.spritesheet = BuildGrid(fileName, width, height, grid.x, grid.y);
#pragma warning restore 618
    }

    private static SpriteMetaData[] BuildGrid(string fileName, int width, int height, int columns, int rows)
    {
        var sprites = new SpriteMetaData[columns * rows];
        string baseName = System.IO.Path.GetFileNameWithoutExtension(fileName);
        int index = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int x = column * width / columns;
                int nextX = (column + 1) * width / columns;
                int top = row * height / rows;
                int bottom = (row + 1) * height / rows;
                int y = height - bottom;
                int sliceWidth = nextX - x;
                int sliceHeight = bottom - top;

                sprites[index] = new SpriteMetaData
                {
                    name = $"{baseName}_{index:D2}",
                    rect = new Rect(x, Math.Max(0, y), sliceWidth, sliceHeight),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                };
                index++;
            }
        }

        return sprites;
    }
}
