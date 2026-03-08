using UnityEngine;

namespace NGames.Settings
{
    /// <summary>
    /// Global narrative runtime configuration.
    /// Create via: Assets > Create > NGames > Narrative Config
    /// </summary>
    [CreateAssetMenu(fileName = "NarrativeConfig", menuName = "NGames/Narrative Config")]
    public class NarrativeConfig : ScriptableObject
    {
        [Header("Text Display")]
        [Tooltip("Delay in seconds between characters when type-writer effect is active.")]
        public float TypewriterCharDelay = 0.03f;

        [Tooltip("Pause in seconds after a sentence-ending punctuation.")]
        public float SentencePauseDelay = 0.15f;

        public bool  TypewriterEnabled  = true;

        [Header("Auto-Advance")]
        public bool  AutoAdvanceEnabled = false;
        public float AutoAdvanceDelay   = 3.0f;

        [Header("Save")]
        [Tooltip("Save slot key prefix used in PlayerPrefs / file system.")]
        public string SaveKeyPrefix = "ngames_save";

        public int MaxSaveSlots = 3;

        [Header("Content Delivery")]
        [Tooltip("Base URL for remote ink.json episode bundles (leave empty for local-only).")]
        public string RemoteContentBaseUrl = "";

        public bool UseAddressablesForEpisodes = false;

        [Header("Debug")]
        public bool VerboseLogging = false;
    }
}
