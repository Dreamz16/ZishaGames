using System;
using NGames.Core.Audio;
using NGames.Core.Narrative;
using NGames.Core.State;
using NGames.Episodes;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NGames.UI
{
    /// <summary>
    /// Creates and manages the in-game pause overlay entirely at runtime.
    /// Attach to _Systems. ESC or the gear button toggles it.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        private GameObject _overlay;
        private GameObject _mainPanel;
        private GameObject _settingsPanel;
        private GameObject _savePanel;
        private GameObject _loadPanel;
        private bool       _visible;

        private void Start()
        {
            // Defer one frame so Canvas is guaranteed to exist
            StartCoroutine(BuildUINextFrame());
        }

        private System.Collections.IEnumerator BuildUINextFrame()
        {
            yield return null;
            var canvas = FindFirstObjectByType<Canvas>()?.transform;
            if (canvas != null) BuildUI(canvas);
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Toggle();
        }

        // ── Public ────────────────────────────────────────────────────────────
        public void Toggle() => SetVisible(!_visible);

        private void SetVisible(bool v)
        {
            _visible = v;
            if (_overlay != null)       _overlay.SetActive(v);
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_savePanel != null)     _savePanel.SetActive(false);
            if (_loadPanel != null)     _loadPanel.SetActive(false);
        }

        // ── UI Construction ───────────────────────────────────────────────────
        private void BuildUI(Transform canvas)
        {
            // ── Full-screen dim overlay ──
            _overlay = MakePanel(canvas.gameObject, "PauseOverlay",
                Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.78f));

            // ── Main pause panel ──
            _mainPanel = MakePanel(_overlay, "MainPanel",
                new Vector2(0.33f, 0.12f), new Vector2(0.67f, 0.92f),
                new Color(0.05f, 0.03f, 0.14f, 0.98f));

            MakeLabel(_mainPanel, "PAUSED", 28, new Color(0.88f, 0.78f, 1f),
                new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.99f), FontStyles.Bold);
            MakePanel(_mainPanel, "Div",
                new Vector2(0.05f, 0.853f), new Vector2(0.95f, 0.859f),
                new Color(0.6f, 0.35f, 1f, 0.55f));

            float y = 0.73f; const float h = 0.090f, gap = 0.016f;
            MakeBtn(_mainPanel, "Resume",    y, h, new Color(0.28f, 0.16f, 0.56f), OnResume);     y -= h + gap;
            MakeBtn(_mainPanel, "Save Game", y, h, new Color(0.12f, 0.08f, 0.26f), OnShowSave);   y -= h + gap;
            MakeBtn(_mainPanel, "Load Game", y, h, new Color(0.12f, 0.08f, 0.26f), OnShowLoad);   y -= h + gap;
            MakeBtn(_mainPanel, "Settings",  y, h, new Color(0.12f, 0.08f, 0.26f), OnSettings);   y -= h + gap;
            MakeBtn(_mainPanel, "Main Menu", y, h, new Color(0.32f, 0.05f, 0.05f), OnMainMenu);

            // ── Sub-panels ──
            _settingsPanel = BuildSettingsPanel(_overlay);
            _savePanel     = BuildSlotPanel(_overlay, "Save", OnSaveSlot);
            _loadPanel     = BuildSlotPanel(_overlay, "Load", OnLoadSlot);

            _settingsPanel.SetActive(false);
            _savePanel.SetActive(false);
            _loadPanel.SetActive(false);

            // ── Always-visible gear button ──
            var gearGo  = MakePanel(canvas.gameObject, "GearBtn",
                new Vector2(0.955f, 0.944f), new Vector2(1f, 1f),
                new Color(0.15f, 0.10f, 0.28f, 0.80f));
            var gearBtn = gearGo.AddComponent<Button>();
            gearBtn.targetGraphic = gearGo.GetComponent<Image>();
            gearBtn.onClick.AddListener(Toggle);
            MakeLabel(gearGo, "⚙", 22, new Color(0.85f, 0.75f, 1f), Vector2.zero, Vector2.one);

            _overlay.SetActive(false);
        }

        private GameObject BuildSettingsPanel(GameObject parent)
        {
            var sp = MakePanel(parent, "SettingsPanel",
                new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.86f),
                new Color(0.04f, 0.02f, 0.13f, 0.99f));

            MakeLabel(sp, "Settings", 26, new Color(0.88f, 0.78f, 1f),
                new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.99f), FontStyles.Bold);
            MakePanel(sp, "Div",
                new Vector2(0.05f, 0.862f), new Vector2(0.95f, 0.868f),
                new Color(0.6f, 0.35f, 1f, 0.55f));

            float y = 0.73f; const float rowH = 0.10f, gap = 0.04f;

            AddSettingRow(sp, "Music Volume", y, rowH,
                () => AudioManager.Instance?.SetMusicVolume((AudioManager.Instance.MusicVolume) - 0.1f),
                () => AudioManager.Instance?.SetMusicVolume((AudioManager.Instance.MusicVolume) + 0.1f));
            y -= rowH + gap;

            AddSettingRow(sp, "SFX Volume", y, rowH,
                () => AudioManager.Instance?.SetSfxVolume((AudioManager.Instance.SfxVolume) - 0.1f),
                () => AudioManager.Instance?.SetSfxVolume((AudioManager.Instance.SfxVolume) + 0.1f));
            y -= rowH + gap;

            var cfg = Resources.Load<NGames.Settings.NarrativeConfig>("Config/NarrativeConfig");
            AddSettingRow(sp, "Text Speed", y, rowH,
                () => { if (cfg) cfg.TypewriterCharDelay = Mathf.Clamp(cfg.TypewriterCharDelay + 0.01f, 0.01f, 0.12f); },
                () => { if (cfg) cfg.TypewriterCharDelay = Mathf.Clamp(cfg.TypewriterCharDelay - 0.01f, 0.01f, 0.12f); },
                "Slower", "Faster");

            MakeBtn(sp, "Back", 0.05f, 0.10f, new Color(0.22f, 0.12f, 0.36f),
                () => sp.SetActive(false));
            return sp;
        }

        private GameObject BuildSlotPanel(GameObject parent, string mode, Action<int> onSlot)
        {
            var sp = MakePanel(parent, $"{mode}SlotPanel",
                new Vector2(0.28f, 0.25f), new Vector2(0.72f, 0.80f),
                new Color(0.04f, 0.02f, 0.13f, 0.99f));

            MakeLabel(sp, $"{mode} Game", 24, new Color(0.88f, 0.78f, 1f),
                new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.98f), FontStyles.Bold);
            MakePanel(sp, "Div",
                new Vector2(0.05f, 0.825f), new Vector2(0.95f, 0.831f),
                new Color(0.6f, 0.35f, 1f, 0.55f));

            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                int slot = i;
                float y = 0.60f - i * 0.18f;
                var slotLabel = SaveSystem.GetSlotLabel(slot);
                var color = SaveSystem.SlotExists(slot)
                    ? new Color(0.18f, 0.10f, 0.36f)
                    : new Color(0.10f, 0.06f, 0.20f);

                var btnGo = MakePanel(sp, $"Slot{slot}", new Vector2(0.06f, y), new Vector2(0.94f, y + 0.14f), color);
                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnGo.GetComponent<Image>();
                btn.onClick.AddListener(() => onSlot(slot));
                MakeLabel(btnGo, $"Slot {slot + 1}  ·  {slotLabel}", 15, Color.white,
                    Vector2.zero, Vector2.one);
            }

            MakeBtn(sp, "Cancel", 0.04f, 0.10f, new Color(0.22f, 0.12f, 0.36f),
                () => sp.SetActive(false));
            return sp;
        }

        // ── Handlers ──────────────────────────────────────────────────────────
        private void OnResume()   => SetVisible(false);
        private void OnSettings() { _settingsPanel.SetActive(true); }
        private void OnMainMenu() { SetVisible(false); SceneManager.LoadScene("MainMenu"); }

        private void OnShowSave()
        {
            // Refresh slot labels
            Destroy(_savePanel);
            _savePanel = BuildSlotPanel(_overlay, "Save", OnSaveSlot);
            _savePanel.SetActive(true);
        }

        private void OnShowLoad()
        {
            Destroy(_loadPanel);
            _loadPanel = BuildSlotPanel(_overlay, "Load", OnLoadSlot);
            _loadPanel.SetActive(true);
        }

        private void OnSaveSlot(int slot)
        {
            var epId    = NarrativeManager.Instance?.CurrentEpisodeId ?? "unknown";
            var epTitle = epId;
            SaveSystem.SaveToSlot(slot, epId, epTitle);
            _savePanel.SetActive(false);
        }

        private void OnLoadSlot(int slot)
        {
            if (!SaveSystem.SlotExists(slot)) return;
            if (!SaveSystem.LoadFromSlot(slot, out var episodeId, out var storyJson)) return;

            SetVisible(false);
            var registry = Resources.Load<EpisodeRegistry>("Episodes/EpisodeRegistry");
            if (registry == null) return;
            EpisodeManifest ep = null;
            foreach (var e in registry.Episodes)
                if (e.EpisodeId == episodeId) { ep = e; break; }
            if (ep != null)
                EpisodeLoader.Instance?.LoadAndPlayEpisodeAsync(ep, storyJson);
        }

        // ── UI Helpers ────────────────────────────────────────────────────────
        private void AddSettingRow(GameObject parent, string labelText, float yMin, float rowH,
            Action onMinus, Action onPlus, string minLbl = "−", string plusLbl = "+")
        {
            MakeLabel(parent, labelText, 16, Color.white,
                new Vector2(0.04f, yMin), new Vector2(0.58f, yMin + rowH));

            var m = MakePanel(parent, "Minus", new Vector2(0.60f, yMin + 0.01f), new Vector2(0.76f, yMin + rowH - 0.01f),
                new Color(0.25f, 0.14f, 0.42f));
            var mb = m.AddComponent<Button>(); mb.targetGraphic = m.GetComponent<Image>();
            mb.onClick.AddListener(() => onMinus());
            MakeLabel(m, minLbl, 18, Color.white, Vector2.zero, Vector2.one);

            var p = MakePanel(parent, "Plus", new Vector2(0.80f, yMin + 0.01f), new Vector2(0.96f, yMin + rowH - 0.01f),
                new Color(0.25f, 0.14f, 0.42f));
            var pb = p.AddComponent<Button>(); pb.targetGraphic = p.GetComponent<Image>();
            pb.onClick.AddListener(() => onPlus());
            MakeLabel(p, plusLbl, 18, Color.white, Vector2.zero, Vector2.one);
        }

        private static GameObject MakePanel(GameObject parent, string name,
            Vector2 aMin, Vector2 aMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<Image>().color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        private static void MakeLabel(GameObject parent, string text, float size, Color color,
            Vector2 aMin, Vector2 aMax, FontStyles style = FontStyles.Normal)
        {
            var go  = new GameObject("Label");
            go.transform.SetParent(parent.transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.color     = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void MakeBtn(GameObject parent, string label,
            float yMin, float height, Color color, Action onClick)
        {
            var go  = MakePanel(parent, label + "_Btn", new Vector2(0.07f, yMin), new Vector2(0.93f, yMin + height), color);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            btn.onClick.AddListener(() => onClick());
            MakeLabel(go, label, 18, Color.white, Vector2.zero, Vector2.one);
        }
    }
}
