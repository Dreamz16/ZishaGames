using UnityEngine;
using UnityEditor;
using System.IO;

public class FixAndGeneratePrefabs
{
    [MenuItem("Tools/Fix and Generate 3D Prefabs")]
    public static void Generate()
    {
        FixTextures("Assets/Resources/Characters");
        FixTextures("Assets/Resources/Backgrounds");
        
        GenerateSprites("Characters", "Characters3D", new Vector3(0, -1f, 3f), new Vector3(1, 1, 1));
        GenerateSprites("Backgrounds", "Scenes3D", new Vector3(0, 0, 10f), new Vector3(1, 1, 1));
        AssetDatabase.SaveAssets();
        Debug.Log("Finished fixing and generating prefabs.");
    }

    private static void FixTextures(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;
        string[] files = Directory.GetFiles(folderPath, "*.*");
        foreach (string file in files)
        {
            if (file.EndsWith(".meta")) continue;
            
            TextureImporter importer = AssetImporter.GetAtPath(file) as TextureImporter;
            if (importer != null)
            {
                if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    // Prevent blurriness
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    
                    if (folderPath.Contains("Characters")) {
                        // Character anchor at bottom
                        importer.spritePivot = new Vector2(0.5f, 0f);
                    } else {
                        // Background anchor at center
                        importer.spritePivot = new Vector2(0.5f, 0.5f);
                    }
                    importer.SaveAndReimport();
                }
            }
        }
    }

    private static void GenerateSprites(string sourceFolder, string targetFolder, Vector3 position, Vector3 scale)
    {
        string sourcePath = "Assets/Resources/" + sourceFolder;
        string targetPath = "Assets/Resources/" + targetFolder;

        if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
        if (!Directory.Exists(sourcePath)) return;

        string[] files = Directory.GetFiles(sourcePath, "*.*");
        foreach (string file in files)
        {
            if (file.EndsWith(".meta")) continue;
            string name = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrEmpty(name)) continue;

            string prefabPath = targetPath + "/" + name + ".prefab";
            
            GameObject go = new GameObject(name);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file);
            if (sprite != null)
            {
                sr.sprite = sprite;
            }
            else
            {
                Debug.LogWarning("Failed to load sprite for " + file);
            }

            go.transform.position = position;
            go.transform.localScale = scale;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            GameObject.DestroyImmediate(go);
        }
    }
}
