using System.Collections.Generic;
using System.Text.RegularExpressions;
using NGames.Core.Events;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace NGames.UI
{
    /// <summary>
    /// Self-bootstrapping character voice system.
    /// Uses OS TTS (macOS: `say`, Windows: PowerShell/SAPI, WebGL: Web Speech API).
    /// Speaks only tagged character lines — narrator text is always silent.
    /// Reads parenthetical stage directions ("(angry) ...") to shape pitch and rate.
    /// </summary>
    public class VoiceManager : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<VoiceManager>() != null) return;
            var go = new GameObject("[VoiceManager]");
            DontDestroyOnLoad(go);
            go.AddComponent<VoiceManager>();
        }

        // ── Per-character voice configuration ─────────────────────────────────
        private struct VoiceConfig
        {
            public string MacOSVoice;
            public int    Rate;    // words per minute
            public float  Pitch;  // 0.40 (very deep) – 1.60 (high); 1.00 = neutral
        }

        // ── Emotion keyword table ──────────────────────────────────────────────
        // (keyword, rate multiplier, pitch delta)
        private static readonly (string kw, float rateMult, float pitchDelta)[] EmotionMap =
        {
            ("fast",       1.35f,  0.05f),
            ("quick",      1.35f,  0.05f),
            ("urgent",     1.30f,  0.05f),
            ("angry",      1.12f, -0.10f),
            ("fierce",     1.15f, -0.12f),
            ("growl",      1.00f, -0.18f),
            ("snarl",      0.98f, -0.20f),
            ("furious",    1.20f, -0.10f),
            ("tense",      1.20f,  0.05f),
            ("anxious",    1.15f,  0.06f),
            ("strained",   1.10f,  0.04f),
            ("surprised",  0.90f,  0.15f),
            ("shocked",    0.85f,  0.18f),
            ("sad",        0.78f, -0.10f),
            ("resigned",   0.72f, -0.12f),
            ("hollow",     0.75f, -0.08f),
            ("flat",       0.80f, -0.06f),
            ("slow",       0.68f, -0.05f),
            ("careful",    0.75f,  0.00f),
            ("measured",   0.82f,  0.00f),
            ("quiet",      0.78f, -0.05f),
            ("whisper",    0.68f, -0.06f),
            ("soft",       0.78f,  0.00f),
            ("barely",     0.72f, -0.03f),
            ("warm",       0.90f,  0.02f),
            ("gentle",     0.85f,  0.02f),
            ("calm",       0.85f,  0.00f),
            ("bitter",     1.05f,  0.08f),
            ("sharp",      1.05f,  0.08f),
            ("cutting",    1.05f,  0.07f),
            ("dry",        0.95f,  0.00f),
            ("laughing",   1.10f,  0.12f),
            ("amused",     1.05f,  0.10f),
            ("wry",        0.95f,  0.03f),
            ("low",        0.82f, -0.07f),
            ("broken",     0.70f, -0.12f),
        };

        private static readonly Regex StageDirectionRe =
            new Regex(@"^(""?)\(([^)]+)\)\s*", RegexOptions.Compiled);

        // ── Per-character voice configuration ─────────────────────────────────
        private static readonly Dictionary<string, VoiceConfig> CharVoices = new()
        {
            // Fang — female werewolf: husky, assertive, fast. Zoe is an expressive
            //   US female voice; pitch pulled below neutral for a feral edge.
            //   (Download via System Preferences > Accessibility > Spoken Content if needed.)
            { "fang",       new VoiceConfig { MacOSVoice = "Zoe",      Rate = 188, Pitch = 0.82f } },

            // Marcus — man-bear: Fred is the deepest macOS voice; very slow, deliberate.
            //   Pitch floored for genuine sub-bass rumble.
            { "marcus",     new VoiceConfig { MacOSVoice = "Fred",     Rate =  88, Pitch = 0.62f } },

            // Lawrence — Korean male: Yuna is macOS's Korean voice (female timbre).
            //   Pitch pulled down to read more masculine while keeping the accent.
            { "lawrence",   new VoiceConfig { MacOSVoice = "Yuna",     Rate = 152, Pitch = 0.78f } },

            // Tiberius — weathered bartender: Tom has a warm, measured baritone.
            //   Slightly lowered pitch for that "seen-it-all" gravelly quality.
            { "tiberius",   new VoiceConfig { MacOSVoice = "Tom",      Rate = 118, Pitch = 0.80f } },

            // Ishani — protagonist
            { "ishani",     new VoiceConfig { MacOSVoice = "Samantha", Rate = 165, Pitch = 1.00f } },

            // Elemental spirits — Victoria carries an ethereal lilt
            { "water",      new VoiceConfig { MacOSVoice = "Victoria", Rate = 132, Pitch = 1.12f } },
            { "sky",        new VoiceConfig { MacOSVoice = "Victoria", Rate = 140, Pitch = 1.18f } },
            { "stone",      new VoiceConfig { MacOSVoice = "Fred",     Rate =  95, Pitch = 0.55f } },

            // Supporting cast
            { "tidewarden", new VoiceConfig { MacOSVoice = "Tom",      Rate = 128, Pitch = 0.88f } },
            { "sera",       new VoiceConfig { MacOSVoice = "Kate",     Rate = 155, Pitch = 1.02f } },
            { "tariq",      new VoiceConfig { MacOSVoice = "Alex",     Rate = 142, Pitch = 0.92f } },
            { "batu",       new VoiceConfig { MacOSVoice = "Alex",     Rate = 118, Pitch = 0.88f } },
            { "kira",       new VoiceConfig { MacOSVoice = "Samantha", Rate = 192, Pitch = 1.08f } },
            { "yildiz",     new VoiceConfig { MacOSVoice = "Karen",    Rate = 155, Pitch = 1.00f } },
            { "almas",      new VoiceConfig { MacOSVoice = "Victoria", Rate = 145, Pitch = 1.14f } },
            { "warden",     new VoiceConfig { MacOSVoice = "Tom",      Rate = 125, Pitch = 0.85f } },
            { "player",     new VoiceConfig { MacOSVoice = "Samantha", Rate = 162, Pitch = 1.00f } },
        };

        private static readonly VoiceConfig DefaultFemale =
            new() { MacOSVoice = "Samantha", Rate = 158, Pitch = 1.00f };
        private static readonly VoiceConfig DefaultMale   =
            new() { MacOSVoice = "Alex",     Rate = 145, Pitch = 0.95f };

        // ── State ─────────────────────────────────────────────────────────────
        private string _currentSpeaker = string.Empty;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        private System.Diagnostics.Process _sayProcess;
#endif

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void OnEnable()
        {
            GameEventBus.Subscribe<SpeakerChangedEvent>(OnSpeaker);
            GameEventBus.Subscribe<StoryLineReadEvent>(OnLine);
            GameEventBus.Subscribe<SceneTransitionEvent>(_ => StopSpeech());
            GameEventBus.Subscribe<StoryEndedEvent>(_ => StopSpeech());
            GameEventBus.Subscribe<ChoicePresentedEvent>(_ => StopSpeech());
        }

        private void OnDisable()
        {
            GameEventBus.Unsubscribe<SpeakerChangedEvent>(OnSpeaker);
            GameEventBus.Unsubscribe<StoryLineReadEvent>(OnLine);
            GameEventBus.Unsubscribe<SceneTransitionEvent>(_ => StopSpeech());
            GameEventBus.Unsubscribe<StoryEndedEvent>(_ => StopSpeech());
            GameEventBus.Unsubscribe<ChoicePresentedEvent>(_ => StopSpeech());
        }

        private void OnDestroy() => StopSpeech();

        // ── Event handlers ─────────────────────────────────────────────────────
        private void OnSpeaker(SpeakerChangedEvent ev)
        {
            _currentSpeaker = ev.SpeakerName?.ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrEmpty(_currentSpeaker)) StopSpeech();
        }

        private void OnLine(StoryLineReadEvent ev)
        {
            if (string.IsNullOrEmpty(_currentSpeaker)) return;
            Speak(ev.Text, _currentSpeaker);
        }

        // ── Speech dispatch ────────────────────────────────────────────────────
        private void Speak(string rawText, string characterKey)
        {
            StopSpeech();
            var text = StripMarkup(rawText);
            if (string.IsNullOrWhiteSpace(text)) return;

            var cfg  = CharVoices.TryGetValue(characterKey, out var v) ? v : DefaultFemale;
            text     = ExtractEmotion(text, ref cfg);

#if UNITY_WEBGL && !UNITY_EDITOR
            JS_SpeakWebGL(text, cfg.Rate, cfg.Pitch);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            SpeakMacOS(text, cfg.MacOSVoice, cfg.Rate, cfg.Pitch);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            SpeakWindows(text, cfg.Rate);
#endif
        }

        // ── Emotion extraction ─────────────────────────────────────────────────
        /// <summary>
        /// Reads a leading stage direction — e.g. "(angry) Get out." or
        /// "(genuinely surprised) That should have worked." — strips it from
        /// the spoken text, and adjusts Rate/Pitch on the config copy.
        /// </summary>
        private static string ExtractEmotion(string text, ref VoiceConfig cfg)
        {
            var m = StageDirectionRe.Match(text);
            if (!m.Success) return text;

            string openQuote = m.Groups[1].Value;           // "" or empty
            string cue       = m.Groups[2].Value            // e.g. "(angry)"
                                .Trim('(', ')')
                                .ToLowerInvariant();
            string remainder = text.Substring(m.Length);

            // Re-attach the opening quote the regex consumed
            if (openQuote.Length > 0 && !remainder.StartsWith("\""))
                remainder = "\"" + remainder;

            float rateMult   = 1.0f;
            float pitchDelta = 0.0f;
            bool  anyMatch   = false;

            foreach (var (kw, rm, pd) in EmotionMap)
            {
                if (cue.Contains(kw))
                {
                    rateMult   += (rm - 1.0f);  // accumulate deltas
                    pitchDelta += pd;
                    anyMatch    = true;
                }
            }

            if (anyMatch)
            {
                cfg.Rate  = Mathf.RoundToInt(cfg.Rate  * Mathf.Clamp(rateMult, 0.50f, 2.00f));
                cfg.Pitch = Mathf.Clamp(cfg.Pitch + pitchDelta, 0.40f, 1.60f);
            }

            return string.IsNullOrWhiteSpace(remainder) ? text : remainder;
        }

        // ── macOS ──────────────────────────────────────────────────────────────
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        private void SpeakMacOS(string text, string voice, int rate, float pitch)
        {
            // Map pitch 0.40–1.60 → [[pbas 10–90]]  (Speech Synthesis Manager command)
            int pbas    = Mathf.RoundToInt(Mathf.Lerp(10f, 90f, (pitch - 0.40f) / 1.20f));
            var escaped = text.Replace("'", "'\\''");
            var args    = $"-v \"{voice}\" -r {rate} '[[pbas {pbas}]]{escaped}'";
            var info    = new System.Diagnostics.ProcessStartInfo("say", args)
            {
                UseShellExecute = false,
                CreateNoWindow  = true,
            };
            _sayProcess = new System.Diagnostics.Process { StartInfo = info };
            try   { _sayProcess.Start(); }
            catch (System.Exception e)
            { Debug.LogWarning($"[VoiceManager] say failed: {e.Message}"); }
        }
#endif

        // ── Windows ────────────────────────────────────────────────────────────
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private void SpeakWindows(string text, int rate)
        {
            int sapiRate = Mathf.RoundToInt(Mathf.Lerp(-10f, 10f, (rate - 80f) / 220f));
            var escaped  = text.Replace("\"", "'");
            var script   = $"Add-Type -AssemblyName System.speech; " +
                           $"$s=New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                           $"$s.Rate={sapiRate}; " +
                           $"$s.Speak(\\\"{escaped}\\\")";
            var info = new System.Diagnostics.ProcessStartInfo("powershell",
                $"-Command \"{script}\"")
            {
                UseShellExecute = false,
                CreateNoWindow  = true,
            };
            try   { new System.Diagnostics.Process { StartInfo = info }.Start(); }
            catch (System.Exception e)
            { Debug.LogWarning($"[VoiceManager] PowerShell TTS failed: {e.Message}"); }
        }
#endif

        // ── Stop ───────────────────────────────────────────────────────────────
        private void StopSpeech()
        {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            if (_sayProcess != null && !_sayProcess.HasExited)
            {
                try { _sayProcess.Kill(); } catch { }
            }
            _sayProcess = null;
#elif UNITY_WEBGL && !UNITY_EDITOR
            JS_StopWebGLSpeech();
#endif
        }

        // ── WebGL JS bridge ────────────────────────────────────────────────────
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void JS_SpeakWebGL(string text, int rate, float pitch);
        [DllImport("__Internal")] private static extern void JS_StopWebGLSpeech();
#endif

        // ── Helpers ────────────────────────────────────────────────────────────
        private static string StripMarkup(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = Regex.Replace(text, "<[^>]+>", "");               // HTML / TMP tags
            text = Regex.Replace(text, @"\s+", " ").Trim();
            return text;
        }
    }
}
