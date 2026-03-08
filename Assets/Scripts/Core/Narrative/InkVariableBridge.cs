using System.Collections.Generic;
using NGames.Core.State;
using UnityEngine;

namespace NGames.Core.Narrative
{
    /// <summary>
    /// Optional component for designers who want to observe or override
    /// Ink variables from the Unity Inspector without writing C#.
    ///
    /// Attach to the NarrativeManager GameObject and configure variable
    /// overrides in the Inspector. They are applied after each episode loads.
    ///
    /// Also useful for debug override during development of a new episode.
    /// </summary>
    public class InkVariableBridge : MonoBehaviour
    {
        [System.Serializable]
        public class VariableOverride
        {
            public string Name;
            public string StringValue;
            public bool   IsInt;
            public bool   IsBool;
            public int    IntValue;
            public bool   BoolValue;
        }

        [Header("Debug Overrides (applied on episode load in Editor only)")]
        [SerializeField] private List<VariableOverride> _overrides = new();

        [Header("Read-only watch (populated at runtime)")]
        [SerializeField] private List<string> _watchedVariables = new();
        [SerializeField] private List<string> _watchedValues    = new();

        private void OnEnable()
        {
            Core.Events.GameEventBus.Subscribe<Core.Events.EpisodeLoadedEvent>(OnEpisodeLoaded);
        }

        private void OnDisable()
        {
            Core.Events.GameEventBus.Unsubscribe<Core.Events.EpisodeLoadedEvent>(OnEpisodeLoaded);
        }

        private void OnEpisodeLoaded(Core.Events.EpisodeLoadedEvent ev)
        {
#if UNITY_EDITOR
            ApplyOverrides();
#endif
        }

        private void ApplyOverrides()
        {
            var nm = NarrativeManager.Instance;
            if (nm == null) return;

            foreach (var ov in _overrides)
            {
                if (string.IsNullOrEmpty(ov.Name)) continue;
                if (ov.IsBool)      nm.SetVariable(ov.Name, ov.BoolValue);
                else if (ov.IsInt)  nm.SetVariable(ov.Name, ov.IntValue);
                else                nm.SetVariable(ov.Name, ov.StringValue);
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            RefreshWatch();
#endif
        }

        private void RefreshWatch()
        {
            var nm = NarrativeManager.Instance;
            if (nm == null || !nm.IsStoryActive) return;

            _watchedValues.Clear();
            foreach (var name in _watchedVariables)
            {
                var val = nm.GetVariable(name);
                _watchedValues.Add($"{name} = {val ?? "null"}");
            }
        }
    }
}
