using System;
using System.Collections.Generic;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NGames.UI
{
    /// <summary>
    /// Pure UI component — only reads data pushed to it by DialogueController.
    /// No direct coupling to NarrativeManager or GameEventBus.
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        [Header("Dialogue")]
        [SerializeField] private TextMeshProUGUI _dialogueText;
        [SerializeField] private TextMeshProUGUI _speakerNameText;
        [SerializeField] private Image           _speakerPortrait;
        [SerializeField] private GameObject      _dialoguePanel;

        [Header("Choices")]
        [SerializeField] private Transform       _choicesContainer;
        [SerializeField] private ChoiceButtonView _choiceButtonPrefab;

        [Header("End Screen")]
        [SerializeField] private GameObject      _endPanel;
        [SerializeField] private Button          _nextEpisodeButton;
        [SerializeField] private Button          _mainMenuButton;

        private readonly List<ChoiceButtonView> _choiceButtons = new();

        [Header("Advance Indicator")]
        [SerializeField] private GameObject _advanceIndicator;

        private void Awake()
        {
            // Self-wire if Inspector references are missing
            if (_dialogueText    == null) _dialogueText    = FindTMP("DialogueText");
            if (_speakerNameText == null) _speakerNameText = FindTMP("SpeakerRow/SpeakerName");
            if (_speakerNameText == null) _speakerNameText = FindTMP("SpeakerName");
            if (_choicesContainer == null)
            {
                var t = transform.parent?.Find("ChoicesContainer");
                if (t != null) _choicesContainer = t;
            }
            if (_endPanel == null)
            {
                var t = transform.parent?.Find("EndPanel");
                if (t != null) _endPanel = t.gameObject;
            }
            if (_choiceButtonPrefab == null)
            {
                var prefab = Resources.Load<GameObject>("ChoiceButton");
                if (prefab != null) _choiceButtonPrefab = prefab.GetComponent<ChoiceButtonView>();
            }
            if (_advanceIndicator == null)
            {
                var t = transform.Find("AdvanceIndicator");
                if (t != null) _advanceIndicator = t.gameObject;
            }
        }

        private TextMeshProUGUI FindTMP(string childName)
        {
            var t = transform.Find(childName);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        // ── Dialogue ───────────────────────────────────────────────────────────
        public void SetDialogueText(string text)
        {
            if (_dialogueText != null) _dialogueText.text = text;
        }

        public void ShowAdvanceIndicator(bool show)
        {
            if (_advanceIndicator != null) _advanceIndicator.SetActive(show);
        }

        public void SetAdvanceAlpha(float alpha)
        {
            if (_advanceIndicator == null) return;
            var cg = _advanceIndicator.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = alpha; return; }
            var tmp = _advanceIndicator.GetComponent<TextMeshProUGUI>();
            if (tmp != null) { var c = tmp.color; tmp.color = new Color(c.r, c.g, c.b, alpha); return; }
            var img = _advanceIndicator.GetComponent<UnityEngine.UI.Image>();
            if (img != null) { var c = img.color; img.color = new Color(c.r, c.g, c.b, alpha); }
        }

        // ── TMP Typewriter helpers ─────────────────────────────────────────────
        public void SetMaxVisibleCharacters(int count)
        {
            if (_dialogueText != null) _dialogueText.maxVisibleCharacters = count;
        }

        public int GetVisibleCharacterCount()
        {
            if (_dialogueText == null) return 0;
            _dialogueText.ForceMeshUpdate();
            return _dialogueText.textInfo.characterCount;
        }

        public char GetCharAt(int index)
        {
            if (_dialogueText == null || index < 0) return '\0';
            var info = _dialogueText.textInfo;
            if (index >= info.characterCount) return '\0';
            return info.characterInfo[index].character;
        }

        public void ShowAllCharacters()
        {
            if (_dialogueText != null) _dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        public void SetSpeakerName(string name)
        {
            if (_speakerNameText != null)
            {
                _speakerNameText.text    = name;
                _speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(name));
            }
        }

        public void SetPortrait(string portraitKey)
        {
            if (_speakerPortrait == null) return;
            var sprite = Resources.Load<Sprite>($"Portraits/{portraitKey}");
            _speakerPortrait.sprite = sprite;
            _speakerPortrait.gameObject.SetActive(sprite != null);
        }

        // ── Choices ────────────────────────────────────────────────────────────
        public void ShowChoices(List<Choice> choices, Action<int> onChoiceSelected)
        {
            HideChoices();

            for (int i = 0; i < choices.Count; i++)
            {
                var btn = Instantiate(_choiceButtonPrefab, _choicesContainer);
                btn.Setup(i, choices[i].text, onChoiceSelected);
                _choiceButtons.Add(btn);
            }

            _choicesContainer.gameObject.SetActive(true);
        }

        public void HideChoices()
        {
            foreach (var btn in _choiceButtons) Destroy(btn.gameObject);
            _choiceButtons.Clear();

            if (_choicesContainer != null)
                _choicesContainer.gameObject.SetActive(false);
        }

        // ── End Screen ─────────────────────────────────────────────────────────
        public void ShowEndPanel()
        {
            if (_endPanel != null) _endPanel.SetActive(true);
        }

        public void HideEndPanel()
        {
            if (_endPanel != null) _endPanel.SetActive(false);
        }

        public void SetNextEpisodeAction(Action action)
        {
            if (_nextEpisodeButton == null) return;
            _nextEpisodeButton.onClick.RemoveAllListeners();
            _nextEpisodeButton.onClick.AddListener(() => action());
        }

        public void SetMainMenuAction(Action action)
        {
            if (_mainMenuButton == null) return;
            _mainMenuButton.onClick.RemoveAllListeners();
            _mainMenuButton.onClick.AddListener(() => action());
        }
    }
}
