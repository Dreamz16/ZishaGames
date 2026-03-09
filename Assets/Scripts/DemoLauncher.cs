using NGames.Core.Narrative;
using NGames.Core.State;
using NGames.Episodes;
using UnityEngine;
using UnityEngine.UI;

public class DemoLauncher : MonoBehaviour
{
    [SerializeField] private EpisodeRegistry _registry;

    private EpisodeManifest _episode;

    private async void Start()
    {
        if (_registry == null)
            _registry = Resources.Load<EpisodeRegistry>("Episodes/EpisodeRegistry");

        _episode = _registry?.GetByNumber(1);
        if (_episode == null) { Debug.LogError("[DemoLauncher] No episode found."); return; }

        WirePlayAgainButton();

        // Check if we're resuming from a save
        int pending = SaveSystem.PendingLoadSlot;
        if (pending >= 0 && SaveSystem.LoadFromSlot(pending, out var episodeId, out var storyJson))
        {
            SaveSystem.PendingLoadSlot = -1;
            EpisodeManifest ep = null;
            foreach (var e in _registry.Episodes)
                if (e.EpisodeId == episodeId) { ep = e; break; }

            if (ep != null)
            {
                await EpisodeLoader.Instance.LoadAndPlayEpisodeAsync(ep, storyJson);
                return;
            }
        }

        // Player character is always Ishani
        var gsm = GameStateManager.Instance;
        if (gsm != null) gsm.SaveData.PlayerName = "Fortune";

        await EpisodeLoader.Instance.LoadAndPlayEpisodeAsync(_episode);
    }

    private void WirePlayAgainButton()
    {
        var canvas = FindFirstObjectByType<Canvas>()?.transform;
        if (canvas == null) return;
        var btn = canvas.Find("EndPanel/PlayAgainButton")?.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnPlayAgain);
    }

    private async void OnPlayAgain()
    {
        var canvas = FindFirstObjectByType<Canvas>()?.transform;
        canvas?.Find("EndPanel")?.gameObject.SetActive(false);
        if (_episode != null)
            await EpisodeLoader.Instance.LoadAndPlayEpisodeAsync(_episode);
    }
}
