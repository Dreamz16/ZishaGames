using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGames.Core.Events
{
    /// <summary>
    /// Lightweight, type-safe pub/sub event bus.
    /// Decouples narrative systems from UI and game systems without requiring
    /// direct references or Unity Inspector wiring.
    ///
    /// Usage:
    ///   // Subscribe
    ///   GameEventBus.Subscribe<SpeakerChangedEvent>(OnSpeakerChanged);
    ///
    ///   // Publish
    ///   GameEventBus.Publish(new SpeakerChangedEvent { SpeakerName = "Alice" });
    ///
    ///   // Unsubscribe (always do this in OnDestroy)
    ///   GameEventBus.Unsubscribe<SpeakerChangedEvent>(OnSpeakerChanged);
    /// </summary>
    public static class GameEventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_handlers.ContainsKey(type))
                _handlers[type] = new List<Delegate>();

            _handlers[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var list))
                list.Remove(handler);
        }

        public static void Publish<T>(T eventData)
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list)) return;

            // Iterate over a copy so handlers can safely unsubscribe during dispatch
            foreach (var handler in list.ToArray())
            {
                try
                {
                    ((Action<T>)handler)(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameEventBus] Handler error for {type.Name}: {ex}");
                }
            }
        }

        /// <summary>Clears all subscriptions. Call on scene unload if needed.</summary>
        public static void Clear()
        {
            _handlers.Clear();
        }
    }
}
