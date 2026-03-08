using System;
using NGames.Core.Events;
using NGames.Core.Narrative;
using NGames.Settings;
using UnityEngine;

namespace NGames.Core.State
{
    /// <summary>
    /// Manages all persistent player state across episodes and sessions.
    ///
    /// Persists to:
    ///   - PlayerPrefs (WebGL / quick save)
    ///   - Application.persistentDataPath JSON file (iOS / Android)
    ///
    /// The active slot is determined by PlatformManager at runtime.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [SerializeField] private NarrativeConfig _config;

        public SaveData SaveData   { get; private set; } = new();
        public string   PlayerName => SaveData.PlayerName;

        private const int SchemaVersion = 1;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromDisk(slotIndex: 0);
        }

        private void OnEnable()
        {
            GameEventBus.Subscribe<EpisodeCompletedEvent>(OnEpisodeCompleted);
        }

        private void OnDisable()
        {
            GameEventBus.Unsubscribe<EpisodeCompletedEvent>(OnEpisodeCompleted);
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveToDisk(); }
        private void OnApplicationQuit()             { SaveToDisk(); }

        // ── Episode progress ───────────────────────────────────────────────────
        public bool HasCompletedEpisode(string episodeId)
            => !string.IsNullOrEmpty(episodeId) && SaveData.CompletedEpisodes.Contains(episodeId);

        public void MarkEpisodeCompleted(string episodeId)
        {
            if (string.IsNullOrEmpty(episodeId)) return;
            SaveData.CompletedEpisodes.Add(episodeId);
            SaveData.LastPlayedEpisodeId = episodeId;
            SaveCurrentEpisodeState(episodeId);
            SaveToDisk();
        }

        public void SaveCurrentEpisodeState(string episodeId)
        {
            var json = NarrativeManager.Instance?.GetStoryStateJson();
            if (!string.IsNullOrEmpty(json))
                SaveData.EpisodeStates[episodeId] = json;
        }

        public string GetEpisodeState(string episodeId)
            => SaveData.EpisodeStates.TryGetValue(episodeId, out var s) ? s : null;

        // ── Flags / Counters / Strings ─────────────────────────────────────────
        public bool GetFlag(string key)                => SaveData.Flags.TryGetValue(key, out var v) && v;
        public void SetFlag(string key, bool value)    { SaveData.Flags[key] = value; }

        public int  GetCounter(string key)             => SaveData.Counters.TryGetValue(key, out var v) ? v : 0;
        public int  IncrementCounter(string key)       { SaveData.Counters[key] = GetCounter(key) + 1; return SaveData.Counters[key]; }
        public void SetCounter(string key, int value)  { SaveData.Counters[key] = value; }

        public string GetString(string key)            => SaveData.Strings.TryGetValue(key, out var v) ? v : string.Empty;
        public void   SetString(string key, string v)  { SaveData.Strings[key] = v; }

        // ── Serialization ──────────────────────────────────────────────────────
        public void SaveToDisk(int slotIndex = 0)
        {
            SaveData.LastSavedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var json = JsonUtility.ToJson(SaveData, prettyPrint: false);

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                PlayerPrefs.SetString(SaveKey(slotIndex), json);
                PlayerPrefs.Save();
            }
            else
            {
                var path = SavePath(slotIndex);
                System.IO.File.WriteAllText(path, json);
            }

            Debug.Log($"[GameStateManager] Saved to slot {slotIndex}.");
        }

        public void LoadFromDisk(int slotIndex = 0)
        {
            string json = null;

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                json = PlayerPrefs.GetString(SaveKey(slotIndex), null);
            }
            else
            {
                var path = SavePath(slotIndex);
                if (System.IO.File.Exists(path))
                    json = System.IO.File.ReadAllText(path);
            }

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    SaveData = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log($"[GameStateManager] Loaded slot {slotIndex}.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameStateManager] Corrupt save in slot {slotIndex}: {ex.Message}. Starting fresh.");
                    SaveData = new SaveData();
                }
            }
            else
            {
                SaveData = new SaveData();
            }
        }

        public void DeleteSlot(int slotIndex)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                PlayerPrefs.DeleteKey(SaveKey(slotIndex));
            else
            {
                var path = SavePath(slotIndex);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            SaveData = new SaveData();
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private string SaveKey(int slot) => $"{_config?.SaveKeyPrefix ?? "ngames_save"}_{slot}";
        private string SavePath(int slot) => System.IO.Path.Combine(
            Application.persistentDataPath, $"{_config?.SaveKeyPrefix ?? "ngames_save"}_{slot}.json");

        private void OnEpisodeCompleted(EpisodeCompletedEvent ev)
            => MarkEpisodeCompleted(ev.EpisodeId);
    }
}
