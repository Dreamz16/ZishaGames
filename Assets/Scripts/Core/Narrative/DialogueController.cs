using System.Collections;
using NGames.Core.Events;
using NGames.Settings;
using NGames.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NGames.Core.Narrative
{
    [RequireComponent(typeof(DialogueView))]
    public class DialogueController : MonoBehaviour
    {
        [SerializeField] private NarrativeConfig _config;
        [SerializeField] private DialogueView    _view;

        private bool      _isTyping;
        private Coroutine _typewriterCoroutine;
        private Coroutine _pulseCoroutine;
        private string    _currentFullText;
        private bool      _awaitingChoice;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (_view == null) _view = GetComponent<DialogueView>();
        }

        private void OnEnable()
        {
            GameEventBus.Subscribe<StoryLineReadEvent>(OnLineRead);
            GameEventBus.Subscribe<ChoicePresentedEvent>(OnChoicesPresented);
            GameEventBus.Subscribe<StoryEndedEvent>(OnStoryEnded);
            GameEventBus.Subscribe<SpeakerChangedEvent>(OnSpeakerChanged);
        }

        private void OnDisable()
        {
            GameEventBus.Unsubscribe<StoryLineReadEvent>(OnLineRead);
            GameEventBus.Unsubscribe<ChoicePresentedEvent>(OnChoicesPresented);
            GameEventBus.Unsubscribe<StoryEndedEvent>(OnStoryEnded);
            GameEventBus.Unsubscribe<SpeakerChangedEvent>(OnSpeakerChanged);
        }

        // ── Input ──────────────────────────────────────────────────────────────
        private void Update()
        {
            if (_awaitingChoice) return;

            bool advance =
                (Keyboard.current != null && (
                    Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Keyboard.current.enterKey.wasPressedThisFrame ||
                    Keyboard.current.xKey.wasPressedThisFrame)) ||
                (Mouse.current   != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);

            if (!advance) return;

            if (_isTyping)
                SkipTypewriter();
            else if (NarrativeManager.Instance != null && NarrativeManager.Instance.CanContinue)
                NarrativeManager.Instance.ContinueStory();
        }

        // ── Event Handlers ─────────────────────────────────────────────────────
        private void OnLineRead(StoryLineReadEvent ev)
        {
            _awaitingChoice  = false;
            _currentFullText = ev.Text;
            _view.HideChoices();
            SetAdvance(false);

            if (_config?.TypewriterEnabled == true)
                StartTypewriter(ev.Text);
            else
            {
                _view.SetDialogueText(ev.Text);
                _view.ShowAllCharacters();
                SetAdvance(NarrativeManager.Instance?.CanContinue == true);
            }

            _view.AnimateLineIn();

            if (_config?.AutoAdvanceEnabled == true && !_awaitingChoice)
                StartCoroutine(AutoAdvance());
        }

        private void OnChoicesPresented(ChoicePresentedEvent ev)
        {
            _awaitingChoice = true;
            StopTypewriterIfRunning();
            _view.ShowAllCharacters();
            _view.SetDialogueText(_currentFullText);
            SetAdvance(false);
            _view.ShowChoices(ev.Choices, OnChoiceSelected);
        }

        private void OnChoiceSelected(int index)
        {
            _awaitingChoice = false;
            _view.HideChoices();
            NarrativeManager.Instance?.MakeChoice(index);
        }

        private void OnStoryEnded(StoryEndedEvent ev)
        {
            _awaitingChoice = false;
            SetAdvance(false);
            _view.ShowEndPanel();
        }

        private void OnSpeakerChanged(SpeakerChangedEvent ev)
        {
            _view.SetSpeakerName(ev.SpeakerName ?? string.Empty);
            if (!string.IsNullOrEmpty(ev.PortraitKey))
                _view.SetPortrait(ev.PortraitKey);
        }

        // ── Typewriter (TMP-native: maxVisibleCharacters) ──────────────────────
        private void StartTypewriter(string text)
        {
            StopTypewriterIfRunning();
            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(text));
        }

        private IEnumerator TypewriterRoutine(string text)
        {
            _isTyping = true;
            _view.SetDialogueText(text);   // set full text with markup intact
            _view.SetMaxVisibleCharacters(0);

            // Wait one frame for TMP to parse the text and populate characterCount
            yield return null;

            int total = _view.GetVisibleCharacterCount();
            float charDelay  = _config?.TypewriterCharDelay  ?? 0.03f;
            float pauseDelay = _config?.SentencePauseDelay   ?? 0.15f;

            for (int i = 1; i <= total; i++)
            {
                _view.SetMaxVisibleCharacters(i);

                // Sentence pause: check the character we just revealed
                char revealed = _view.GetCharAt(i - 1);
                float delay = (revealed == '.' || revealed == '!' || revealed == '?')
                    ? charDelay + pauseDelay
                    : charDelay;

                yield return new WaitForSeconds(delay);
            }

            _view.ShowAllCharacters();
            _isTyping = false;
            SetAdvance(NarrativeManager.Instance?.CanContinue == true);
        }

        private void SkipTypewriter()
        {
            StopTypewriterIfRunning();
            _isTyping = false;
            _view.ShowAllCharacters();
            SetAdvance(NarrativeManager.Instance?.CanContinue == true);
        }

        private void StopTypewriterIfRunning()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
            _isTyping = false;
        }

        // ── Advance indicator ──────────────────────────────────────────────────
        private void SetAdvance(bool show)
        {
            _view.ShowAdvanceIndicator(show);
            if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
            if (show) _pulseCoroutine = StartCoroutine(PulseAdvance());
        }

        private IEnumerator PulseAdvance()
        {
            while (true)
            {
                float e = 0;
                while (e < 0.6f) { e += Time.deltaTime; _view.SetAdvanceAlpha(Mathf.Lerp(0.3f, 1f, e / 0.6f)); yield return null; }
                e = 0;
                while (e < 0.4f) { e += Time.deltaTime; _view.SetAdvanceAlpha(Mathf.Lerp(1f, 0.3f, e / 0.4f)); yield return null; }
            }
        }

        private IEnumerator AutoAdvance()
        {
            yield return new WaitForSeconds(_config?.AutoAdvanceDelay ?? 3f);
            if (!_awaitingChoice && !_isTyping && NarrativeManager.Instance?.CanContinue == true)
                NarrativeManager.Instance.ContinueStory();
        }
    }
}
