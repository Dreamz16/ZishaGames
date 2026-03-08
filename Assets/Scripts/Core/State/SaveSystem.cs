using System;
using NGames.Core.Narrative;
using UnityEngine;

namespace NGames.Core.State
{
    /// <summary>3-slot PlayerPrefs save system.</summary>
    public static class SaveSystem
    {
        public const int SlotCount = 3;

        // Set before loading Bootstrap to auto-load a save slot on startup.
        public static int PendingLoadSlot
        {
            get => PlayerPrefs.GetInt("pending_load_slot", -1);
            set { if (value < 0) PlayerPrefs.DeleteKey("pending_load_slot"); else PlayerPrefs.SetInt("pending_load_slot", value); PlayerPrefs.Save(); }
        }

        public static bool SlotExists(int slot) =>
            PlayerPrefs.GetInt($"save_{slot}_exists", 0) == 1;

        public static string GetSlotLabel(int slot) =>
            SlotExists(slot)
                ? PlayerPrefs.GetString($"save_{slot}_label", "Save")
                : "— Empty —";

        public static void SaveToSlot(int slot, string episodeId, string episodeTitle)
        {
            var storyJson = NarrativeManager.Instance?.GetStoryStateJson() ?? string.Empty;
            var gameJson  = GameStateManager.Instance?.SaveData != null
                ? JsonUtility.ToJson(GameStateManager.Instance.SaveData)
                : string.Empty;

            PlayerPrefs.SetInt($"save_{slot}_exists", 1);
            PlayerPrefs.SetString($"save_{slot}_label",   $"{episodeTitle}  —  {DateTime.Now:MMM d · HH:mm}");
            PlayerPrefs.SetString($"save_{slot}_episode", episodeId);
            PlayerPrefs.SetString($"save_{slot}_story",   storyJson);
            PlayerPrefs.SetString($"save_{slot}_game",    gameJson);
            PlayerPrefs.Save();
        }

        public static bool LoadFromSlot(int slot, out string episodeId, out string storyJson)
        {
            episodeId = string.Empty;
            storyJson = string.Empty;
            if (!SlotExists(slot)) return false;

            episodeId = PlayerPrefs.GetString($"save_{slot}_episode", string.Empty);
            storyJson = PlayerPrefs.GetString($"save_{slot}_story",   string.Empty);

            var gameJson = PlayerPrefs.GetString($"save_{slot}_game", string.Empty);
            if (!string.IsNullOrEmpty(gameJson) && GameStateManager.Instance?.SaveData != null)
                JsonUtility.FromJsonOverwrite(gameJson, GameStateManager.Instance.SaveData);

            return !string.IsNullOrEmpty(episodeId);
        }

        public static void DeleteSlot(int slot)
        {
            PlayerPrefs.DeleteKey($"save_{slot}_exists");
            PlayerPrefs.DeleteKey($"save_{slot}_label");
            PlayerPrefs.DeleteKey($"save_{slot}_episode");
            PlayerPrefs.DeleteKey($"save_{slot}_story");
            PlayerPrefs.DeleteKey($"save_{slot}_game");
            PlayerPrefs.Save();
        }
    }
}
