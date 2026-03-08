// AutoSetup.cs — runs once when Unity opens the project.
// Creates NarrativeConfig, EpisodeManifest, EpisodeRegistry, Bootstrap scene,
// and the ChoiceButton prefab. Idempotent: skips assets that already exist.

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using NGames.Settings;
using NGames.Episodes;
using NGames.Core.Narrative;
using NGames.Core.State;
using NGames.Platform;
using NGames.UI;

[InitializeOnLoad]
public static class AutoSetup
{
    static AutoSetup()
    {
        // Defer until after domain reload so all scripts are compiled
        EditorApplication.delayCall += RunSetup;
    }

    [UnityEditor.MenuItem("NGames/Run Setup")]
    public static void RunSetupMenu() => RunSetup();

    [UnityEditor.MenuItem("NGames/Import TMP Essential Resources")]
    public static void ImportTMPResources()
    {
        const string pkg = "/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        if (System.IO.File.Exists(pkg))
        {
            AssetDatabase.ImportPackage(pkg, false);
            Debug.Log("[AutoSetup] TMP Essential Resources imported.");
        }
        else
            Debug.LogError("[AutoSetup] TMP Essential Resources package not found at: " + pkg);
    }

    [UnityEditor.MenuItem("NGames/Patch Panel Positions")]
    public static void PatchPanelPositions()
    {
        const string scenePath = "Assets/Scenes/Bootstrap.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        var canvas = Object.FindFirstObjectByType<Canvas>()?.transform;
        if (canvas == null) { Debug.LogError("[AutoSetup] No Canvas found."); return; }

        var dialoguePanel = canvas.Find("DialoguePanel");
        var choicesContainer = canvas.Find("ChoicesContainer");

        if (dialoguePanel != null)
        {
            var rt = dialoguePanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.16f, 0.28f);
            rt.anchorMax = new Vector2(0.84f, 0.68f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            dialoguePanel.GetComponent<Image>().color = new Color(0.04f, 0.02f, 0.10f, 0.93f);
        }

        if (choicesContainer != null)
        {
            var rt = choicesContainer.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.16f, 0.03f);
            rt.anchorMax = new Vector2(0.84f, 0.26f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AutoSetup] Panel positions updated. Dialogue is now centred between character cards; choices at the bottom.");
    }

    [UnityEditor.MenuItem("NGames/Patch EndPanel (Play Again Button)")]
    public static void PatchEndPanel()
    {
        const string scenePath = "Assets/Scenes/Bootstrap.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        var canvas = Object.FindFirstObjectByType<Canvas>()?.transform;
        if (canvas == null) { Debug.LogError("[AutoSetup] No Canvas found."); return; }

        var endPanel = canvas.Find("EndPanel");
        if (endPanel == null) { Debug.LogError("[AutoSetup] EndPanel not found."); return; }

        if (endPanel.Find("PlayAgainButton") != null)
        { Debug.Log("[AutoSetup] PlayAgainButton already exists."); return; }

        // Reposition EndLabel
        var endLabel = endPanel.Find("EndLabel");
        if (endLabel != null)
        {
            var rt = endLabel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, 0.47f); rt.anchorMax = new Vector2(0.8f, 0.62f);
        }

        // Play Again button
        var playAgainGo = new GameObject("PlayAgainButton");
        playAgainGo.transform.SetParent(endPanel, false);
        var paBtnImg = playAgainGo.AddComponent<Image>();
        paBtnImg.color = new Color(0.25f, 0.15f, 0.50f, 1f);
        var paBtn = playAgainGo.AddComponent<Button>();
        paBtn.targetGraphic = paBtnImg;
        var paBtnRt = playAgainGo.GetComponent<RectTransform>();
        paBtnRt.anchorMin = new Vector2(0.35f, 0.30f); paBtnRt.anchorMax = new Vector2(0.65f, 0.42f);
        paBtnRt.offsetMin = paBtnRt.offsetMax = Vector2.zero;
        MakeTMP(playAgainGo, "Label", "Play Again", 22, Color.white, Vector2.zero, Vector2.one)
            .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AutoSetup] EndPanel patched with Play Again button.");
    }

    [UnityEditor.MenuItem("NGames/Relink Ink Asset")]
    public static void RelinkInkAsset()
    {
        const string manifestPath  = "Assets/ScriptableObjects/Episodes/Stone_of_Commitment.asset";
        const string inkJsonPath   = "Assets/Ink/Episodes/Stone_of_Commitment/Stone_of_Commitment.ink.json";

        var manifest = AssetDatabase.LoadAssetAtPath<EpisodeManifest>(manifestPath);
        if (manifest == null) { Debug.LogError("[AutoSetup] EpisodeManifest not found. Run Setup first."); return; }

        var inkAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(inkJsonPath);
        if (inkAsset == null) { Debug.LogError($"[AutoSetup] ink.json not found at {inkJsonPath}"); return; }

        manifest.InkAsset = inkAsset;
        EditorUtility.SetDirty(manifest);
        AssetDatabase.SaveAssets();
        Debug.Log($"[AutoSetup] Relinked ink asset: {inkJsonPath}");
    }

    static void RunSetup()
    {
        EditorApplication.delayCall -= RunSetup;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        bool changed = false;
        changed |= CreateNarrativeConfig();
        changed |= CreateEpisodeManifest();
        changed |= CreateEpisodeRegistry();
        changed |= CreateChoiceButtonPrefab();
        changed |= CreateBootstrapScene();

        if (changed)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AutoSetup] Project setup complete. Open Bootstrap scene and press Play.");
        }
        else
        {
            Debug.Log("[AutoSetup] All assets already exist.");
        }
    }

    // ── NarrativeConfig ──────────────────────────────────────────────────────
    static bool CreateNarrativeConfig()
    {
        const string path = "Assets/ScriptableObjects/Config/NarrativeConfig.asset";
        if (File.Exists(path)) return false;

        var config = ScriptableObject.CreateInstance<NarrativeConfig>();
        config.TypewriterEnabled    = true;
        config.TypewriterCharDelay  = 0.03f;
        config.SentencePauseDelay   = 0.15f;
        config.AutoAdvanceEnabled   = false;
        config.AutoAdvanceDelay     = 3f;
        config.SaveKeyPrefix        = "ngames_save";
        config.MaxSaveSlots         = 3;
        config.VerboseLogging       = true;  // on during development

        Directory.CreateDirectory("Assets/ScriptableObjects/Config");
        AssetDatabase.CreateAsset(config, path);
        Debug.Log("[AutoSetup] Created NarrativeConfig.");
        return true;
    }

    // ── EpisodeManifest ──────────────────────────────────────────────────────
    static bool CreateEpisodeManifest()
    {
        const string path = "Assets/ScriptableObjects/Episodes/Stone_of_Commitment.asset";
        if (File.Exists(path)) return false;

        var manifest = ScriptableObject.CreateInstance<EpisodeManifest>();
        manifest.EpisodeId     = "stone_of_commitment";
        manifest.EpisodeNumber = 1;
        manifest.EpisodeTitle  = "Stone of Commitment";
        manifest.EpisodeSummary = "A consent-first romantasy RPG set along the Silk Road.";
        manifest.IsAvailable   = true;
        manifest.EstimatedReadMinutes = 60;

        // Find the compiled ink.json
        var inkGuids = AssetDatabase.FindAssets("Stone_of_Commitment t:TextAsset");
        foreach (var guid in inkGuids)
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith(".ink.json"))
            {
                manifest.InkAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
                Debug.Log($"[AutoSetup] Linked ink asset: {p}");
                break;
            }
        }

        Directory.CreateDirectory("Assets/ScriptableObjects/Episodes");
        AssetDatabase.CreateAsset(manifest, path);
        Debug.Log("[AutoSetup] Created EpisodeManifest.");
        return true;
    }

    // ── EpisodeRegistry ──────────────────────────────────────────────────────
    static bool CreateEpisodeRegistry()
    {
        const string path = "Assets/ScriptableObjects/Episodes/EpisodeRegistry.asset";
        if (File.Exists(path)) return false;

        var registry = ScriptableObject.CreateInstance<EpisodeRegistry>();

        var manifest = AssetDatabase.LoadAssetAtPath<EpisodeManifest>(
            "Assets/ScriptableObjects/Episodes/Stone_of_Commitment.asset");
        if (manifest != null)
            registry.Episodes.Add(manifest);

        AssetDatabase.CreateAsset(registry, path);
        Debug.Log("[AutoSetup] Created EpisodeRegistry.");
        return true;
    }

    // ── ChoiceButton Prefab ──────────────────────────────────────────────────
    static bool CreateChoiceButtonPrefab()
    {
        const string prefabPath = "Assets/Prefabs/UI/ChoiceButton.prefab";
        if (File.Exists(prefabPath)) return false;

        Directory.CreateDirectory("Assets/Prefabs/UI");

        // Root: Button
        var root = new GameObject("ChoiceButton");
        var btn  = root.AddComponent<Button>();
        var img  = root.AddComponent<Image>();
        img.color = new Color(0.12f, 0.08f, 0.25f, 0.9f);

        // Size
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(700, 50);

        // Label child
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = "Choice";
        tmp.fontSize  = 18;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 5);
        labelRect.offsetMax = new Vector2(-10, -5);

        // ChoiceButtonView component
        var view = root.AddComponent<ChoiceButtonView>();

        // Wire via SerializedObject so private fields are set
        var so     = new SerializedObject(view);
        var labelProp  = so.FindProperty("_label");
        var btnProp    = so.FindProperty("_button");
        if (labelProp != null) labelProp.objectReferenceValue = tmp;
        if (btnProp   != null) btnProp.objectReferenceValue   = btn;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Wire button target graphic
        btn.targetGraphic = img;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log($"[AutoSetup] Created ChoiceButton prefab at {prefabPath}");
        return true;
    }

    // ── Bootstrap Scene ──────────────────────────────────────────────────────
    static bool CreateBootstrapScene()
    {
        const string scenePath = "Assets/Scenes/Bootstrap.unity";
        if (File.Exists(scenePath)) return false;

        Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Load assets
        var config   = AssetDatabase.LoadAssetAtPath<NarrativeConfig>(
            "Assets/ScriptableObjects/Config/NarrativeConfig.asset");
        var registry = AssetDatabase.LoadAssetAtPath<EpisodeRegistry>(
            "Assets/ScriptableObjects/Episodes/EpisodeRegistry.asset");
        var choicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/UI/ChoiceButton.prefab");

        // ── Systems root ─────────────────────────────────────────────────────
        var systems = new GameObject("_Systems");

        // PlatformManager
        systems.AddComponent<PlatformManager>();

        // GameStateManager
        var gsm = systems.AddComponent<GameStateManager>();
        SetSerializedField(gsm, "_config", config);

        // NarrativeManager
        var nm = systems.AddComponent<NarrativeManager>();
        SetSerializedField(nm, "_config", config);

        // EpisodeLoader
        var el = systems.AddComponent<EpisodeLoader>();
        SetSerializedField(el, "_config", config);

        // Bootstrap launcher
        var launcher = systems.AddComponent<DemoLauncher>();
        SetSerializedField(launcher, "_registry", registry);

        // ── Camera ───────────────────────────────────────────────────────────
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var cam = cameraGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.10f, 0.20f, 1f);
        cam.orthographic = true;

        // ── Canvas ────────────────────────────────────────────────────────────
        var canvasGo = new GameObject("Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // EventSystem
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        // ── Background (two-panel gradient) ───────────────────────────────────
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        FullStretch(bgGo);

        var bgTop = new GameObject("TopHalf");
        bgTop.transform.SetParent(bgGo.transform, false);
        bgTop.AddComponent<Image>().color = new Color(0.04f, 0.02f, 0.14f, 1f);
        var bgTopRt = bgTop.GetComponent<RectTransform>();
        bgTopRt.anchorMin = new Vector2(0, 0.5f); bgTopRt.anchorMax = Vector2.one;
        bgTopRt.offsetMin = bgTopRt.offsetMax = Vector2.zero;

        var bgBot = new GameObject("BotHalf");
        bgBot.transform.SetParent(bgGo.transform, false);
        bgBot.AddComponent<Image>().color = new Color(0.10f, 0.06f, 0.24f, 1f);
        var bgBotRt = bgBot.GetComponent<RectTransform>();
        bgBotRt.anchorMin = Vector2.zero; bgBotRt.anchorMax = new Vector2(1, 0.5f);
        bgBotRt.offsetMin = bgBotRt.offsetMax = Vector2.zero;

        // ── SceneNameCard ─────────────────────────────────────────────────────
        var cardGo = new GameObject("SceneNameCard");
        cardGo.transform.SetParent(canvasGo.transform, false);
        cardGo.AddComponent<Image>().color = Color.clear; // forces RectTransform creation
        var cardCg = cardGo.AddComponent<CanvasGroup>();
        cardCg.alpha = 0;
        var cardRt = cardGo.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.1f, 0.80f); cardRt.anchorMax = new Vector2(0.9f, 0.95f);
        cardRt.offsetMin = cardRt.offsetMax = Vector2.zero;
        var cardText = MakeTMP(cardGo, "SceneNameText", "", 28, new Color(0.9f, 0.85f, 0.6f),
            Vector2.zero, Vector2.one);
        cardText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // ── Character Area ────────────────────────────────────────────────────
        var charArea = new GameObject("CharacterArea");
        charArea.transform.SetParent(canvasGo.transform, false);
        var charRt = charArea.AddComponent<RectTransform>();
        charRt.anchorMin = new Vector2(0, 0.38f); charRt.anchorMax = new Vector2(1, 0.90f);
        charRt.offsetMin = charRt.offsetMax = Vector2.zero;

        var leftSlot = MakeSlot(charArea, "LeftSlot",  new Vector2(0.01f, 0), new Vector2(0.20f, 1));
        var rightSlot= MakeSlot(charArea, "RightSlot", new Vector2(0.80f, 0), new Vector2(0.99f, 1));

        // ── DialoguePanel — centred between character cards ───────────────────
        var panelGo = new GameObject("DialoguePanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0.04f, 0.02f, 0.10f, 0.93f);
        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.16f, 0.28f); panelRt.anchorMax = new Vector2(0.84f, 0.68f);
        panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;

        // Accent bar (coloured by speaker)
        var accentGo = new GameObject("AccentBar");
        accentGo.transform.SetParent(panelGo.transform, false);
        var accentImg = accentGo.AddComponent<Image>();
        accentImg.color = new Color(0.40f, 0.30f, 0.60f, 1f);
        var accentRt = accentGo.GetComponent<RectTransform>();
        accentRt.anchorMin = new Vector2(0, 0.94f); accentRt.anchorMax = Vector2.one;
        accentRt.offsetMin = accentRt.offsetMax = Vector2.zero;

        // SpeakerRow
        var rowGo = new GameObject("SpeakerRow");
        rowGo.transform.SetParent(panelGo.transform, false);
        var rowRt = rowGo.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.01f, 0.72f); rowRt.anchorMax = new Vector2(0.65f, 0.93f);
        rowRt.offsetMin = rowRt.offsetMax = Vector2.zero;

        // Portrait circle
        var portGo = new GameObject("PortraitIcon");
        portGo.transform.SetParent(rowGo.transform, false);
        var portImg = portGo.AddComponent<Image>();
        portImg.color = new Color(0.40f, 0.30f, 0.60f, 1f);
        var portRt = portGo.GetComponent<RectTransform>();
        portRt.anchorMin = new Vector2(0, 0.05f); portRt.anchorMax = new Vector2(0.18f, 0.95f);
        portRt.offsetMin = portRt.offsetMax = Vector2.zero;

        // SpeakerName TMP
        var speakerGo = MakeTMP(rowGo, "SpeakerName", "", 20, Color.white,
            new Vector2(0.22f, 0), new Vector2(1, 1));
        speakerGo.GetComponent<TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

        // DialogueText
        var dialogueGo = MakeTMP(panelGo, "DialogueText", "", 19, new Color(0.92f, 0.90f, 0.95f),
            new Vector2(0.02f, 0.06f), new Vector2(0.98f, 0.70f));
        dialogueGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;

        // Advance indicator
        var advGo = MakeTMP(panelGo, "AdvanceIndicator", "▼", 16, new Color(0.6f, 0.6f, 0.8f),
            new Vector2(0.88f, 0.02f), new Vector2(1f, 0.16f));
        advGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        advGo.SetActive(false);

        // ── ChoicesContainer ──────────────────────────────────────────────────
        var choicesGo  = new GameObject("ChoicesContainer");
        choicesGo.transform.SetParent(canvasGo.transform, false);
        var choicesRt  = choicesGo.AddComponent<RectTransform>();
        choicesRt.anchorMin = new Vector2(0.16f, 0.03f); choicesRt.anchorMax = new Vector2(0.84f, 0.26f);
        choicesRt.offsetMin = choicesRt.offsetMax = Vector2.zero;
        var vlg = choicesGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10; vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
        choicesGo.SetActive(false);

        // ── EndPanel ──────────────────────────────────────────────────────────
        var endGo = new GameObject("EndPanel");
        endGo.transform.SetParent(canvasGo.transform, false);
        endGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.92f);
        FullStretch(endGo);
        MakeTMP(endGo, "EndLabel", "— End —", 40, Color.white,
            new Vector2(0.2f, 0.47f), new Vector2(0.8f, 0.62f))
            .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Play Again button
        var playAgainGo = new GameObject("PlayAgainButton");
        playAgainGo.transform.SetParent(endGo.transform, false);
        var paBtnImg = playAgainGo.AddComponent<Image>();
        paBtnImg.color = new Color(0.25f, 0.15f, 0.50f, 1f);
        var paBtn = playAgainGo.AddComponent<Button>();
        paBtn.targetGraphic = paBtnImg;
        var paBtnRt = playAgainGo.GetComponent<RectTransform>();
        paBtnRt.anchorMin = new Vector2(0.35f, 0.30f); paBtnRt.anchorMax = new Vector2(0.65f, 0.42f);
        paBtnRt.offsetMin = paBtnRt.offsetMax = Vector2.zero;
        MakeTMP(playAgainGo, "Label", "Play Again", 22, Color.white, Vector2.zero, Vector2.one)
            .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        endGo.SetActive(false);

        // ── DialogueView + DialogueController ────────────────────────────────
        var dv = panelGo.AddComponent<DialogueView>();
        SetSerializedField(dv, "_dialogueText",      dialogueGo.GetComponent<TextMeshProUGUI>());
        SetSerializedField(dv, "_speakerNameText",   speakerGo.GetComponent<TextMeshProUGUI>());
        SetSerializedField(dv, "_choicesContainer",  choicesGo.transform);
        SetSerializedField(dv, "_choiceButtonPrefab",choicePrefab?.GetComponent<ChoiceButtonView>());
        SetSerializedField(dv, "_endPanel",          endGo);
        SetSerializedField(dv, "_advanceIndicator",  advGo);

        var dc = panelGo.AddComponent<DialogueController>();
        SetSerializedField(dc, "_config", config);
        SetSerializedField(dc, "_view",   dv);

        // ── CharacterDisplayManager + SceneBackgroundController ───────────────
        systems.AddComponent<NGames.UI.CharacterDisplayManager>();
        systems.AddComponent<NGames.UI.SceneBackgroundController>();

        // Save scene
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[AutoSetup] Created Bootstrap scene at {scenePath}");
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static void FullStretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            // Adding Image forces Unity to create a RectTransform
            go.AddComponent<Image>().color = Color.clear;
            rt = go.GetComponent<RectTransform>();
        }
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static GameObject MakeSlot(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        // Adding Image forces RectTransform creation
        go.AddComponent<Image>().color = Color.clear;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    static GameObject MakeTMP(GameObject parent, string name, string text,
        float size, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp  = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = new Vector2(15, 5);
        r.offsetMax = new Vector2(-15, -5);
        return go;
    }

    static void SetSerializedField(Object target, string fieldName, Object value)
    {
        if (target == null || value == null) return;
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
