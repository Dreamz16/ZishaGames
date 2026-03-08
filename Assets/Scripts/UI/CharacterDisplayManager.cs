using NGames.Core.Events;
using NGames.Core.State;
using UnityEngine;

namespace NGames.UI
{
    /// <summary>
    /// Sets the character portrait inside DialogueView.
    /// During dialogue: shows the speaking character.
    /// During choices: shows the player character (Ishani).
    /// </summary>
    public class CharacterDisplayManager : MonoBehaviour
    {
        private DialogueView _view;

        private void Start()
        {
            _view = FindFirstObjectByType<DialogueView>();
        }

        private void OnEnable()
        {
            GameEventBus.Subscribe<SpeakerChangedEvent>(OnSpeaker);
            GameEventBus.Subscribe<ChoicePresentedEvent>(OnChoices);
            GameEventBus.Subscribe<StoryEndedEvent>(OnEnd);
            GameEventBus.Subscribe<SceneTransitionEvent>(OnScene);
        }

        private void OnDisable()
        {
            GameEventBus.Unsubscribe<SpeakerChangedEvent>(OnSpeaker);
            GameEventBus.Unsubscribe<ChoicePresentedEvent>(OnChoices);
            GameEventBus.Unsubscribe<StoryEndedEvent>(OnEnd);
            GameEventBus.Unsubscribe<SceneTransitionEvent>(OnScene);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void OnSpeaker(SpeakerChangedEvent ev)
        {
            EnsureView();
            if (string.IsNullOrEmpty(ev.SpeakerName))
            {
                _view?.ShowCharacterImage(false);
                return;
            }
            ShowCharacter(ev.SpeakerName.ToLowerInvariant());
        }

        private void OnChoices(ChoicePresentedEvent _)
        {
            EnsureView();
            var playerName = GameStateManager.Instance?.SaveData?.PlayerName ?? "ishani";
            ShowCharacter(playerName.ToLowerInvariant());
        }

        private void OnEnd(StoryEndedEvent _)        => _view?.ShowCharacterImage(false);
        private void OnScene(SceneTransitionEvent _) => _view?.ShowCharacterImage(false);

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ShowCharacter(string key)
        {
            if (_view == null) return;
            var tex    = Resources.Load<Texture2D>($"Characters/{key}");
            var sprite = tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                new Vector2(0.5f, 0f), 100f)
                : null;
            _view.SetCharacterSprite(sprite);
            _view.ShowCharacterImage(sprite != null);
        }

        private void EnsureView()
        {
            if (_view == null) _view = FindFirstObjectByType<DialogueView>();
        }
    }
}
