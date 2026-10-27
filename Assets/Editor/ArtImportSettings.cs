using UnityEditor;
using UnityEngine;

public class ArtImportSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.Contains("/Resources/Art/"))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteExtrude = 1;
        settings.spritePixelsPerUnit = 80f;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.alphaIsTransparency = true;
        settings.mipmapEnabled = false;
        settings.readable = true;
        settings.filterMode = FilterMode.Bilinear;
        importer.SetTextureSettings(settings);
    }
}

public static class FinalLeapValidate
{
    public static void Run()
    {
        string[] paths =
        {
            "Assets/Resources/Art/title.png",
            "Assets/Resources/Art/background.png",
            "Assets/Resources/Art/Knight/k_r00_01.png",
            "Assets/Resources/Art/Owl/s_r00_03.png",
            "Assets/Resources/Art/Tiles/s_r00_00.png"
        };
        foreach (string path in paths)
        {
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
            UnityEngine.Debug.Log("CHECK " + path + " sprite=" + (sprite != null) + " readable=" + (tex != null && tex.isReadable) + " size=" + (tex != null ? tex.width + "x" + tex.height : "0"));
        }
        UnityEditor.EditorApplication.Exit(0);
    }
}
