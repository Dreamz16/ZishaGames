using System;
using System.Threading.Tasks;
using NGames.Episodes;
using NGames.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NGames.Core.Narrative
{
    /// <summary>
    /// Handles loading of episode ink assets from two sources:
    ///   1. Local TextAsset (bundled with the build — used for launch or offline)
    ///   2. Addressables remote (downloaded on demand — used for weekly drops without
    ///      an app-store update on mobile, or CDN delivery on web)
    ///
    /// Call LoadEpisodeAsync and await the returned EpisodeManifest-with-asset result.
    /// </summary>
    public class EpisodeLoader : MonoBehaviour
    {
        public static EpisodeLoader Instance { get; private set; }

        [SerializeField] private NarrativeConfig _config;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (_config == null)
                _config = Resources.Load<NarrativeConfig>("Config/NarrativeConfig");
        }

        /// <summary>
        /// Ensures the episode's InkAsset is available (downloading via Addressables
        /// if needed), then loads it into the NarrativeManager.
        /// </summary>
        public async Task LoadAndPlayEpisodeAsync(EpisodeManifest episode, string savedStateJson = null,
            Action<float> onProgress = null)
        {
            if (episode == null) throw new ArgumentNullException(nameof(episode));

            TextAsset inkAsset = episode.InkAsset;

            // If no local asset and Addressables is enabled, download it
            if (inkAsset == null && _config?.UseAddressablesForEpisodes == true)
            {
                inkAsset = await LoadRemoteInkAsset(episode, onProgress);
                if (inkAsset == null)
                {
                    Debug.LogError($"[EpisodeLoader] Failed to load remote ink asset for {episode.EpisodeId}");
                    return;
                }
                // Temporarily assign so NarrativeManager can read it
                episode.InkAsset = inkAsset;
            }

            if (inkAsset == null)
            {
                // Fallback: try Resources/Ink/<episodeId>
                inkAsset = Resources.Load<TextAsset>($"Ink/{episode.EpisodeId}");
                if (inkAsset != null)
                    Debug.Log($"[EpisodeLoader] Loaded ink asset from Resources for {episode.EpisodeId}");
            }

            if (inkAsset == null)
            {
                Debug.LogError($"[EpisodeLoader] No ink asset available for episode {episode.EpisodeId}");
                return;
            }

            episode.InkAsset = inkAsset;

            NarrativeManager.Instance?.LoadEpisode(episode, savedStateJson);
            NarrativeManager.Instance?.ContinueStory();
        }

        private async Task<TextAsset> LoadRemoteInkAsset(EpisodeManifest episode, Action<float> onProgress)
        {
            if (episode.RemoteInkAsset == null || !episode.RemoteInkAsset.RuntimeKeyIsValid())
            {
                Debug.LogWarning($"[EpisodeLoader] No valid Addressable key for {episode.EpisodeId}");
                return null;
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(episode.RemoteInkAsset);

            while (!handle.IsDone)
            {
                onProgress?.Invoke(handle.PercentComplete);
                await Task.Yield();
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }

            Debug.LogError($"[EpisodeLoader] Addressables load failed: {handle.OperationException}");
            Addressables.Release(handle);
            return null;
        }
    }
}
