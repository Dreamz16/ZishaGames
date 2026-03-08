using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace NGames.Episodes
{
    /// <summary>
    /// ScriptableObject describing a single weekly episode.
    /// Create via: Assets > Create > NGames > Episode Manifest
    /// </summary>
    [CreateAssetMenu(fileName = "Episode_XXX", menuName = "NGames/Episode Manifest")]
    public class EpisodeManifest : ScriptableObject
    {
        [Header("Identity")]
        public string EpisodeId;          // e.g. "ep_001"
        public int    EpisodeNumber;      // 1-based display number
        public string EpisodeTitle;
        [TextArea(2, 4)]
        public string EpisodeSummary;

        [Header("Content")]
        [Tooltip("Compiled .ink.json text asset. Leave null when using Addressables remote.")]
        public TextAsset InkAsset;

        [Tooltip("Addressable key for remote ink.json (used when InkAsset is null).")]
        public AssetReferenceT<TextAsset> RemoteInkAsset;

        [Header("Release")]
        public System.DateTime ReleaseDate;
        public bool            IsAvailable = true;

        [Header("Unlock Conditions")]
        [Tooltip("IDs of episodes that must be completed before this one unlocks.")]
        public List<string> Prerequisites = new();

        [Header("Metadata")]
        public Sprite ThumbnailImage;
        public int    EstimatedReadMinutes = 15;
        public string[] ContentWarnings;
    }
}
