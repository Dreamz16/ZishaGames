using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace NGames.Core.State
{
    /// <summary>Serializable save slot stored per player account.</summary>
    [Serializable]
    public class AccountSaveSlot
    {
        public string EpisodeId;
        public string StoryJson;        // full Ink story state (Story.state.ToJson())
        public string GameJson;         // full SaveData JSON
        public string SceneDescription; // human-readable last location, e.g. "Highwind Palace"
        public long   Timestamp;        // Unix seconds UTC
    }

    /// <summary>
    /// Local player account system.
    ///
    /// Accounts are stored in PlayerPrefs and persist across app launches.
    /// Each account owns one save slot; saves are bridged to SaveSystem slot 0
    /// when the player chooses to continue, so the existing Bootstrap flow is reused.
    ///
    /// Password security: SHA-256 with per-user salt (username + fixed suffix).
    /// Acceptable for a local game account; not intended as a server-side auth layer.
    /// </summary>
    public static class AccountSystem
    {
        private const string AccountsKey    = "ngames_accounts";
        private const string CurrentUserKey = "ngames_current_user";
        private const string SaveKeyPrefix  = "ngames_save";    // must match NarrativeConfig default

        // ── Session ────────────────────────────────────────────────────────────
        public static string CurrentUser => PlayerPrefs.GetString(CurrentUserKey, string.Empty);
        public static bool   IsLoggedIn  => !string.IsNullOrEmpty(CurrentUser);

        // ── Authenticated flag (survives scene loads, reset on logout) ─────────
        public static bool IsAuthenticated { get; private set; }

        // ── Registration ───────────────────────────────────────────────────────
        public static bool Register(string username, string password, out string error)
        {
            error    = string.Empty;
            username = username?.Trim() ?? string.Empty;
            password = password ?? string.Empty;

            if (username.Length < 3)     { error = "Username must be at least 3 characters."; return false; }
            if (password.Length < 4)     { error = "Password must be at least 4 characters."; return false; }
            if (AccountExists(username)) { error = "Username already taken."; return false; }

            var list    = PlayerPrefs.GetString(AccountsKey, string.Empty);
            var updated = string.IsNullOrEmpty(list) ? username : list + "," + username;
            PlayerPrefs.SetString(AccountsKey,        updated);
            PlayerPrefs.SetString(PwKey(username),    Hash(username, password));
            PlayerPrefs.SetString(CurrentUserKey,     username);
            PlayerPrefs.Save();

            IsAuthenticated = true;
            return true;
        }

        // ── Login ──────────────────────────────────────────────────────────────
        public static bool Login(string username, string password, out string error)
        {
            error    = string.Empty;
            username = username?.Trim() ?? string.Empty;

            if (!AccountExists(username))
            { error = "Account not found."; return false; }

            if (PlayerPrefs.GetString(PwKey(username), string.Empty) != Hash(username, password))
            { error = "Incorrect password."; return false; }

            PlayerPrefs.SetString(CurrentUserKey, username);
            PlayerPrefs.Save();
            IsAuthenticated = true;
            return true;
        }

        // ── Logout ─────────────────────────────────────────────────────────────
        public static void Logout()
        {
            PlayerPrefs.DeleteKey(CurrentUserKey);
            PlayerPrefs.Save();
            IsAuthenticated = false;
        }

        // ── Per-user save ──────────────────────────────────────────────────────
        public static bool HasSave()
        {
            var user = CurrentUser;
            return !string.IsNullOrEmpty(user) && PlayerPrefs.HasKey(UserSaveKey(user));
        }

        public static AccountSaveSlot GetSave()
        {
            var user = CurrentUser;
            if (string.IsNullOrEmpty(user)) return null;
            var json = PlayerPrefs.GetString(UserSaveKey(user), string.Empty);
            if (string.IsNullOrEmpty(json)) return null;
            try   { return JsonUtility.FromJson<AccountSaveSlot>(json); }
            catch { return null; }
        }

        public static void WriteSave(string episodeId, string storyJson, string gameJson, string sceneDesc)
        {
            var user = CurrentUser;
            if (string.IsNullOrEmpty(user)) return;

            var slot = new AccountSaveSlot
            {
                EpisodeId        = episodeId,
                StoryJson        = storyJson,
                GameJson         = gameJson,
                SceneDescription = sceneDesc,
                Timestamp        = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            PlayerPrefs.SetString(UserSaveKey(user), JsonUtility.ToJson(slot));
            PlayerPrefs.Save();
        }

        public static void DeleteSave()
        {
            var user = CurrentUser;
            if (string.IsNullOrEmpty(user)) return;
            PlayerPrefs.DeleteKey(UserSaveKey(user));
            PlayerPrefs.Save();
        }

        // ── Bridge to SaveSystem for Bootstrap resume flow ─────────────────────
        /// <summary>
        /// Writes the account save into SaveSystem slot 0 so the existing Bootstrap
        /// scene flow can resume the game without modification.
        /// Also sets SaveSystem.PendingLoadSlot = 0.
        /// </summary>
        public static void PrepareResumeViaSlot0(AccountSaveSlot slot)
        {
            if (slot == null) return;

            // SaveSystem PlayerPrefs keys (slot 0)
            PlayerPrefs.SetInt   ("save_0_exists",  1);
            PlayerPrefs.SetString("save_0_episode", slot.EpisodeId        ?? string.Empty);
            PlayerPrefs.SetString("save_0_story",   slot.StoryJson        ?? string.Empty);
            PlayerPrefs.SetString("save_0_game",    slot.GameJson         ?? string.Empty);
            PlayerPrefs.SetString("save_0_label",   slot.SceneDescription ?? string.Empty);
            PlayerPrefs.Save();

            // GameStateManager reads from a JSON file (non-WebGL) or PlayerPrefs (WebGL)
            if (!string.IsNullOrEmpty(slot.GameJson))
            {
                try
                {
                    var path = System.IO.Path.Combine(
                        Application.persistentDataPath, $"{SaveKeyPrefix}_0.json");
                    System.IO.File.WriteAllText(path, slot.GameJson);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AccountSystem] Could not write game save to disk: {ex.Message}");
                }
                // WebGL / PlayerPrefs fallback
                PlayerPrefs.SetString($"{SaveKeyPrefix}_0", slot.GameJson);
                PlayerPrefs.Save();
            }

            SaveSystem.PendingLoadSlot = 0;
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static string PwKey(string user)      => $"ngames_{user}_pw";
        private static string UserSaveKey(string user) => $"ngames_{user}_save";

        private static bool AccountExists(string username)
        {
            if (string.IsNullOrEmpty(username)) return false;
            var list = PlayerPrefs.GetString(AccountsKey, string.Empty);
            if (string.IsNullOrEmpty(list)) return false;
            foreach (var u in list.Split(','))
                if (string.Equals(u, username, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string Hash(string username, string password)
        {
            using var sha   = SHA256.Create();
            var       bytes = sha.ComputeHash(
                Encoding.UTF8.GetBytes($"{username.ToLowerInvariant()}|{password}|ngames_v1"));
            return Convert.ToBase64String(bytes);
        }
    }
}
