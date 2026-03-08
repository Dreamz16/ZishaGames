using System.Collections;
using System.Collections.Generic;
using NGames.Core.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NGames.UI
{
    /// <summary>
    /// Manages up to two character cards (left/right slots).
    /// One card per slot. Speaking character is bright; others are dimmed.
    /// Finds Canvas children by name — no serialized wiring required.
    /// </summary>
    public class CharacterDisplayManager : MonoBehaviour
    {
        private static readonly Dictionary<string, Color> CharacterColors = new()
        {
            { "lawrence", new Color(0.90f, 0.68f, 0.18f) },
            { "fang",     new Color(0.20f, 0.78f, 0.62f) },
            { "marcus",   new Color(0.45f, 0.55f, 0.90f) },
            { "tiberius", new Color(0.80f, 0.25f, 0.25f) },
            { "kira",     new Color(0.90f, 0.40f, 0.70f) },
            { "almas",    new Color(0.65f, 0.50f, 0.90f) },
            { "batu",     new Color(0.55f, 0.72f, 0.35f) },
            { "jiwon",    new Color(0.85f, 0.55f, 0.30f) },
            { "nadia",    new Color(0.75f, 0.40f, 0.75f) },
        };

        // Slot → (characterKey, canvasGroup, rectTransform)
        private (string key, CanvasGroup cg, RectTransform rt)? _leftCard;
        private (string key, CanvasGroup cg, RectTransform rt)? _rightCard;

        private Coroutine _leftFade;
        private Coroutine _rightFade;

        private Transform _leftSlot;
        private Transform _rightSlot;
        private Image     _portraitIcon;
        private Image     _speakerAccentBar;
        private string    _currentSpeaker = "";

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            var canvas = FindFirstObjectByType<Canvas>()?.transform;
            if (canvas == null) return;

            var area = canvas.Find("CharacterArea");
            if (area != null)
            {
                _leftSlot  = area.Find("LeftSlot");
                _rightSlot = area.Find("RightSlot");
            }

            var panel = canvas.Find("DialoguePanel");
            if (panel != null)
            {
                _portraitIcon     = panel.Find("SpeakerRow/PortraitIcon")?.GetComponent<Image>();
                _speakerAccentBar = panel.Find("AccentBar")?.GetComponent<Image>();
            }
        }

        private void OnEnable()
        {
            GameEventBus.Subscribe<SpeakerChangedEvent>(OnSpeaker);
            GameEventBus.Subscribe<StoryEndedEvent>(OnEnd);
            GameEventBus.Subscribe<SceneTransitionEvent>(OnSceneTransition);
        }

        private void OnDisable()
        {
            GameEventBus.Unsubscribe<SpeakerChangedEvent>(OnSpeaker);
            GameEventBus.Unsubscribe<StoryEndedEvent>(OnEnd);
            GameEventBus.Unsubscribe<SceneTransitionEvent>(OnSceneTransition);
        }

        // ── Handlers ──────────────────────────────────────────────────────────
        private void OnSpeaker(SpeakerChangedEvent ev)
        {
            if (string.IsNullOrEmpty(ev.SpeakerName))
            {
                // Narration — hide all cards
                _currentSpeaker = "";
                if (_leftCard.HasValue)  { if (_leftFade  != null) StopCoroutine(_leftFade);  _leftFade  = StartCoroutine(Fade(_leftCard.Value.cg,  0f, 0.25f)); }
                if (_rightCard.HasValue) { if (_rightFade != null) StopCoroutine(_rightFade); _rightFade = StartCoroutine(Fade(_rightCard.Value.cg, 0f, 0.25f)); }
                return;
            }

            _currentSpeaker = ev.SpeakerName.ToLowerInvariant();
            var color = GetColor(_currentSpeaker);

            if (_portraitIcon    != null) _portraitIcon.color    = color;
            if (_speakerAccentBar != null) _speakerAccentBar.color = color;

            EnsureCard(ev.SpeakerName, color);
            RefreshHighlights();
        }

        private void OnEnd(StoryEndedEvent _) => ClearCards();

        private void OnSceneTransition(SceneTransitionEvent _) => ClearCards();

        private void ClearCards()
        {
            if (_leftCard.HasValue)
            {
                StartCoroutine(FadeOutAndDestroy(_leftCard.Value.cg));
                _leftCard = null;
            }
            if (_rightCard.HasValue)
            {
                StartCoroutine(FadeOutAndDestroy(_rightCard.Value.cg));
                _rightCard = null;
            }
            _currentSpeaker = "";
        }

        // ── Card management ────────────────────────────────────────────────────
        private void EnsureCard(string name, Color color)
        {
            var key = name.ToLowerInvariant();

            // Already exists in a slot
            if ((_leftCard?.key  == key) ||
                (_rightCard?.key == key)) return;

            // Assign to an empty slot
            if (_leftSlot != null && !_leftCard.HasValue)
            {
                var (cg, rt) = BuildCard(name, color, _leftSlot);
                _leftCard = (key, cg, rt);
                StartCoroutine(SlideIn(rt, cg, fromLeft: true));
            }
            else if (_rightSlot != null && !_rightCard.HasValue)
            {
                var (cg, rt) = BuildCard(name, color, _rightSlot);
                _rightCard = (key, cg, rt);
                StartCoroutine(SlideIn(rt, cg, fromLeft: false));
            }
            else
            {
                // Both slots full — evict the inactive (non-speaking) card
                bool evictLeft = _rightCard?.key == _currentSpeaker;
                if (evictLeft)
                {
                    if (_leftCard.HasValue) StartCoroutine(FadeOutAndDestroy(_leftCard.Value.cg));
                    var (cg, rt) = BuildCard(name, color, _leftSlot);
                    _leftCard = (key, cg, rt);
                    StartCoroutine(SlideIn(rt, cg, fromLeft: true));
                }
                else
                {
                    if (_rightCard.HasValue) StartCoroutine(FadeOutAndDestroy(_rightCard.Value.cg));
                    var (cg, rt) = BuildCard(name, color, _rightSlot);
                    _rightCard = (key, cg, rt);
                    StartCoroutine(SlideIn(rt, cg, fromLeft: false));
                }
            }
        }

        private void RefreshHighlights()
        {
            if (_leftCard.HasValue)
            {
                bool active = _leftCard.Value.key == _currentSpeaker;
                if (_leftFade != null) StopCoroutine(_leftFade);
                _leftFade = StartCoroutine(Fade(_leftCard.Value.cg, active ? 1f : 0.20f, 0.3f));
                StartCoroutine(Scale(_leftCard.Value.rt, active ? 1.04f : 0.96f, 0.2f));
            }
            if (_rightCard.HasValue)
            {
                bool active = _rightCard.Value.key == _currentSpeaker;
                if (_rightFade != null) StopCoroutine(_rightFade);
                _rightFade = StartCoroutine(Fade(_rightCard.Value.cg, active ? 1f : 0.20f, 0.3f));
                StartCoroutine(Scale(_rightCard.Value.rt, active ? 1.04f : 0.96f, 0.2f));
            }
        }

        // ── Card builder ──────────────────────────────────────────────────────
        private static (CanvasGroup cg, RectTransform rt) BuildCard(
            string name, Color color, Transform parent)
        {
            var key = name.ToLowerInvariant();
            var tex = Resources.Load<Texture2D>($"Characters/{key}");
            var sprite = tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 100f)
                : null;

            var root = new GameObject(name + "_Card");
            root.transform.SetParent(parent, false);
            root.AddComponent<Image>().color = Color.clear;

            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var cg = root.AddComponent<CanvasGroup>();
            cg.alpha = 0;

            if (sprite != null)
                BuildSpriteCard(root, sprite, color);
            else
                BuildPlaceholderCard(root, name, color);

            return (cg, rt);
        }

        private static void BuildSpriteCard(GameObject root, Sprite sprite, Color accentColor)
        {
            // Subtle coloured rim at the bottom — ties character to their accent colour
            Panel(root, "RimGlow", Vector2.zero, new Vector2(1f, 0.06f),
                new Color(accentColor.r, accentColor.g, accentColor.b, 0.55f));

            // Character sprite — preserve aspect, anchored to bottom so feet stay grounded
            var sprGo = new GameObject("Sprite");
            sprGo.transform.SetParent(root.transform, false);
            var img = sprGo.AddComponent<Image>();
            img.sprite          = sprite;
            img.preserveAspect  = true;
            img.type            = Image.Type.Simple;
            img.raycastTarget   = false;
            var sprRt = sprGo.GetComponent<RectTransform>();
            sprRt.anchorMin = Vector2.zero;
            sprRt.anchorMax = Vector2.one;
            sprRt.offsetMin = sprRt.offsetMax = Vector2.zero;
        }

        private static void BuildPlaceholderCard(GameObject root, string name, Color color)
        {
            // Dark tinted background
            Panel(root, "BG", Vector2.zero, Vector2.one,
                new Color(color.r * 0.10f, color.g * 0.10f, color.b * 0.10f, 0.92f));

            // Top accent bar
            Panel(root, "TopBar", new Vector2(0, 0.94f), Vector2.one, color);

            // Bottom accent bar
            Panel(root, "BotBar", Vector2.zero, new Vector2(1f, 0.03f),
                new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, 1f));

            // Large initial letter
            var initGo = new GameObject("Initial");
            initGo.transform.SetParent(root.transform, false);
            var tmp = initGo.AddComponent<TextMeshProUGUI>();
            tmp.text             = name[..1].ToUpper();
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin      = 24;
            tmp.fontSizeMax      = 120;
            tmp.color            = new Color(color.r, color.g, color.b, 0.18f);
            tmp.alignment        = TextAlignmentOptions.Center;
            tmp.fontStyle        = FontStyles.Bold;
            var initRt = initGo.GetComponent<RectTransform>();
            initRt.anchorMin = new Vector2(0f, 0.10f);
            initRt.anchorMax = new Vector2(1f, 0.92f);
            initRt.offsetMin = initRt.offsetMax = Vector2.zero;

            // Name label at bottom
            var namGo = new GameObject("Name");
            namGo.transform.SetParent(root.transform, false);
            var namTmp = namGo.AddComponent<TextMeshProUGUI>();
            namTmp.text      = name;
            namTmp.fontSize  = 13;
            namTmp.color     = new Color(1f, 1f, 1f, 0.9f);
            namTmp.alignment = TextAlignmentOptions.Center;
            namTmp.fontStyle = FontStyles.Bold;
            var namRt = namGo.GetComponent<RectTransform>();
            namRt.anchorMin = new Vector2(0, 0.04f);
            namRt.anchorMax = new Vector2(1, 0.18f);
            namRt.offsetMin = new Vector2(4, 0);
            namRt.offsetMax = new Vector2(-4, 0);
        }

        private static void Panel(GameObject parent, string n,
            Vector2 aMin, Vector2 aMax, Color color)
        {
            var go = new GameObject(n);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<Image>().color = color;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = aMin; r.anchorMax = aMax;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        // ── Animations ────────────────────────────────────────────────────────
        private static IEnumerator SlideIn(RectTransform rt, CanvasGroup cg, bool fromLeft)
        {
            const float dur = 0.4f;
            float elapsed = 0;
            var startOffset = rt.offsetMin;
            float slideDir = fromLeft ? -30f : 30f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / dur), 2f);
                rt.offsetMin = new Vector2(Mathf.Lerp(slideDir, startOffset.x, t), rt.offsetMin.y);
                cg.alpha = t;
                yield return null;
            }
            rt.offsetMin = startOffset;
            cg.alpha = 1f;
        }

        private static IEnumerator Fade(CanvasGroup cg, float target, float dur)
        {
            float start = cg.alpha, e = 0;
            while (e < dur) { e += Time.deltaTime; cg.alpha = Mathf.Lerp(start, target, e / dur); yield return null; }
            cg.alpha = target;
        }

        private static IEnumerator Scale(RectTransform rt, float target, float dur)
        {
            var s0 = rt.localScale; var s1 = new Vector3(target, target, 1f); float e = 0;
            while (e < dur) { e += Time.deltaTime; rt.localScale = Vector3.Lerp(s0, s1, e / dur); yield return null; }
            rt.localScale = s1;
        }

        private static IEnumerator FadeOutAndDestroy(CanvasGroup cg)
        {
            if (cg == null) yield break;
            float e = 0;
            while (e < 0.5f) { e += Time.deltaTime; cg.alpha = 1f - e / 0.5f; yield return null; }
            if (cg != null) Destroy(cg.gameObject);
        }

        private static Color GetColor(string key)
        {
            foreach (var kvp in CharacterColors)
                if (key.Contains(kvp.Key)) return kvp.Value;
            return new Color(0.7f, 0.7f, 0.8f);
        }
    }
}
