using UnityEngine;
using UnityEditor;
using System.IO;

public class PrefabGenerator
{
    [MenuItem("Tools/Generate 3D Prefabs")]
    public static void GeneratePrefabs()
    {
        // Generate Characters
        string[] characterFiles = Directory.GetFiles("Assets/Resources/Characters", "*.png");
        foreach (var file in characterFiles)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            CreateSpritePrefab("Characters", "Characters3D", name, file);
        }

        // Generate Scenes
        if (Directory.Exists("Assets/Resources/Backgrounds"))
        {
            string[] sceneFiles = Directory.GetFiles("Assets/Resources/Backgrounds", "*.png");
            foreach (var file in sceneFiles)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                CreateSpritePrefab("Backgrounds", "Scenes3D", name, file);
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("Finished generating prefabs.");
    }

    private static void CreateSpritePrefab(string sourceType, string targetDir, string name, string assetPath)
    {
        string prefabPath = $"Assets/Resources/{targetDir}/{name}.prefab";
        if (File.Exists(prefabPath)) return;

        if (!Directory.Exists($"Assets/Resources/{targetDir}"))
        {
            Directory.CreateDirectory($"Assets/Resources/{targetDir}");
        }

        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
        {
            sr.sprite = sprite;
        }

        if (sourceType == "Characters") {
            go.transform.position = new Vector3(0, -1f, 3f);
        } else {
            go.transform.position = new Vector3(0, 0, 10f);
            
            // Adjust scale to cover camera
            go.transform.localScale = new Vector3(20f, 12f, 1f); 
        }

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        GameObject.DestroyImmediate(go);
    }
}
