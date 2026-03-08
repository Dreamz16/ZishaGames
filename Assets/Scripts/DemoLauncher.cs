using NGames.Core.Events;
using NGames.Core.Narrative;
using NGames.Episodes;
using UnityEngine;
using UnityEngine.UI;

public class DemoLauncher : MonoBehaviour
{
    [SerializeField] private EpisodeRegistry _registry;

    private EpisodeManifest _episode;

    async void Start()
    {
        if (_registry == null)
            _registry = Resources.Load<EpisodeRegistry>("Episodes/EpisodeRegistry");

        _episode = _registry?.GetByNumber(1);
        if (_episode == null) { Debug.LogError("[DemoLauncher] No episode found."); return; }

        WirePlayAgainButton();

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
        // Hide end panel and restart
        var canvas = FindFirstObjectByType<Canvas>()?.transform;
        canvas?.Find("EndPanel")?.gameObject.SetActive(false);
        if (_episode != null)
            await EpisodeLoader.Instance.LoadAndPlayEpisodeAsync(_episode);
    }
}
