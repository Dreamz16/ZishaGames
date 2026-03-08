#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class BuildSetup
{
    [MenuItem("NGames/Configure Build Settings")]
    public static void Configure()
    {
        var scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",   true),
            new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity",  true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[BuildSetup] Build scenes configured: MainMenu (0), Bootstrap (1).");
    }
}
#endif
