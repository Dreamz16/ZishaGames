using System;
using System.Collections.Generic;

namespace NGames.Core.State
{
    [Serializable]
    public class SaveData
    {
        public string         PlayerName         = "Player";
        public int            SchemaVersion       = 1;
        public string         LastPlayedEpisodeId = string.Empty;
        public long           LastSavedTimestamp  = 0;

        // Per-episode Ink story state (serialized JSON from Story.state.ToJson())
        public Dictionary<string, string> EpisodeStates     = new();
        public HashSet<string>            CompletedEpisodes = new();

        // Cross-episode persistent flags (written via Ink external functions)
        public Dictionary<string, bool>   Flags    = new();
        public Dictionary<string, int>    Counters = new();
        public Dictionary<string, string> Strings  = new();
    }
}
