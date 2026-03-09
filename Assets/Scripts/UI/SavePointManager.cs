using NGames.Core.Events;
using NGames.Core.Narrative;
using NGames.Core.State;
using UnityEngine;

namespace NGames.UI
{
    /// <summary>
    /// Auto-saves the current game state to the player's account save slot
    /// whenever a scene transition fires or choices are presented.
    /// Silently no-ops if no user is logged in or no story is active.
    /// </summary>
    public class SavePointManager : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<SavePointManager>() != null) return;
            var go = new GameObject("[SavePointManager]");
            DontDestroyOnLoad(go);
            go.AddComponent<SavePointManager>();
        }

        private void OnEnable()
        {
            GameEventBus.Subscribe<SceneTransitionEvent>(OnScene);
            GameEventBus.Subscribe<ChoicePresentedEvent>(OnChoice);
            GameEventBus.Subscribe<StoryEndedEvent>(OnEnd);
        }

        private void OnDisable()
        {
            GameEventBus.Unsubscribe<SceneTransitionEvent>(OnScene);
            GameEventBus.Unsubscribe<ChoicePresentedEvent>(OnChoice);
            GameEventBus.Unsubscribe<StoryEndedEvent>(OnEnd);
        }

        private void OnApplicationQuit() => SaveNow("Last session");

        private void OnScene(SceneTransitionEvent ev)  => SaveNow(FormatScene(ev.SceneName));
        private void OnChoice(ChoicePresentedEvent _)   => SaveNow(CurrentScene());
        private void OnEnd(StoryEndedEvent _)           => SaveNow("Episode complete");

        // ── Core save ──────────────────────────────────────────────────────────
        private static void SaveNow(string sceneDesc)
        {
            if (!AccountSystem.IsLoggedIn) return;

            var nm = NarrativeManager.Instance;
            if (nm == null || !nm.IsStoryActive) return;

            var episodeId = nm.CurrentEpisodeId ?? string.Empty;
            var storyJson = nm.GetStoryStateJson() ?? string.Empty;
            var gameJson  = GameStateManager.Instance?.SaveData != null
                ? JsonUtility.ToJson(GameStateManager.Instance.SaveData)
                : string.Empty;

            AccountSystem.WriteSave(episodeId, storyJson, gameJson, sceneDesc);
        }

        private static string CurrentScene()
        {
            var nm = NarrativeManager.Instance;
            return nm != null ? $"Episode {nm.CurrentEpisodeId}" : "Unknown";
        }

        private static string FormatScene(string raw)
            => System.Text.RegularExpressions.Regex
               .Replace(raw ?? string.Empty, "[_]", " ").Trim();
    }
}
