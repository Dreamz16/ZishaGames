using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NGames.Episodes
{
    /// <summary>
    /// Master catalog of all episodes.
    /// Drag episode manifests here as they are created each week.
    /// Create via: Assets > Create > NGames > Episode Registry
    /// </summary>
    [CreateAssetMenu(fileName = "EpisodeRegistry", menuName = "NGames/Episode Registry")]
    public class EpisodeRegistry : ScriptableObject
    {
        [Tooltip("All episodes in release order. Add new episodes here each week.")]
        public List<EpisodeManifest> Episodes = new();

        public EpisodeManifest GetById(string episodeId)
            => Episodes.FirstOrDefault(e => e.EpisodeId == episodeId);

        public EpisodeManifest GetByNumber(int number)
            => Episodes.FirstOrDefault(e => e.EpisodeNumber == number);

        public List<EpisodeManifest> GetAvailableEpisodes()
            => Episodes.Where(e => e.IsAvailable).ToList();

        public List<EpisodeManifest> GetUnlockedEpisodes(HashSet<string> completedIds)
        {
            return Episodes.Where(e =>
                e.IsAvailable &&
                e.Prerequisites.All(prereq => completedIds.Contains(prereq))
            ).ToList();
        }

        public EpisodeManifest GetNextEpisode(string currentEpisodeId)
        {
            var current = GetById(currentEpisodeId);
            if (current == null) return null;

            return Episodes
                .Where(e => e.EpisodeNumber > current.EpisodeNumber && e.IsAvailable)
                .OrderBy(e => e.EpisodeNumber)
                .FirstOrDefault();
        }
    }
}
