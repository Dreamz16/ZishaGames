using System;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using NGames.Core.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NGames.UI
{
    /// <summary>
    /// Unified bottom panel: character portrait on left, dialogue text or choices on right.
    /// No speaker name or accent bar.
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        [Header("Dialogue")]
        [SerializeField] private TextMeshProUGUI _dialogueText;
        [SerializeField] private GameObject      _dialoguePanel;

        [Header("Choices")]
        [SerializeField] private Transform       _choicesContainer;
        [SerializeField] private ChoiceButtonView _choiceButtonPrefab;

        [Header("End Screen")]
        [SerializeField] private GameObject _endPanel;
        [SerializeField] private Button     _nextEpisodeButton;
        [SerializeField] private Button     _mainMenuButton;

        [Header("Advance Indicator")]
        [SerializeField] private GameObject _advanceIndicator;

        private readonly List<ChoiceButtonView> _choiceButtons = new();

        // Character portrait — created at runtime
        private Image           _characterImage;
        private Image           _placeholderBg;
        private TextMeshProUGUI _placeholderInitial;
        private RawImage        _characterRawImage; // NEW: For 3D RenderTextures
        private RectTransform   _dialogueRt;
        private CanvasGroup     _panelCg;
        private Coroutine       _textAnimRoutine;

        // Anchor presets for dialogue text
        private static readonly Vector2 TextAnchorMinFull    = new(0.02f, 0.06f);
        private static readonly Vector2 TextAnchorMaxFull    = new(0.97f, 0.92f);
        private static readonly Vector2 TextAnchorMinWithChar = new(0.27f, 0.06f);
        private static readonly Vector2 TextAnchorMaxWithChar = new(0.97f, 0.92f);

        private void Awake()
        {
            // Self-wire
            if (_dialogueText == null)     _dialogueText     = FindTMP("DialogueText");
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

            var existingCg = GetComponent<CanvasGroup>();
            _panelCg = (existingCg != null) ? existingCg : gameObject.AddComponent<CanvasGroup>();

            if (_dialogueText != null)
                _dialogueRt = _dialogueText.GetComponent<RectTransform>();

            BuildCharacterImage();
        }

        private void BuildCharacterImage()
        {
            // Shared anchor rect
            var slot = new GameObject("CharacterSlot");
            slot.transform.SetParent(transform, false);
            slot.transform.SetAsFirstSibling();
            var slotRt = slot.AddComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(0f,    0f);
            slotRt.anchorMax = new Vector2(0.25f, 1f);
            slotRt.offsetMin = slotRt.offsetMax = Vector2.zero;
            slot.SetActive(false);

            // Real portrait image
            var imgGo = new GameObject("Portrait");
            imgGo.transform.SetParent(slot.transform, false);
            _characterImage                = imgGo.AddComponent<Image>();
            _characterImage.preserveAspect = true;
            _characterImage.raycastTarget  = false;
            _characterImage.color          = Color.white;
            _characterImage.type           = Image.Type.Simple;
            var imgRt = imgGo.GetComponent<RectTransform>();
            imgRt.anchorMin = Vector2.zero;
            imgRt.anchorMax = Vector2.one;
            imgRt.offsetMin = imgRt.offsetMax = Vector2.zero;

            // 3D Portrait RawImage
            var rawGo = new GameObject("Portrait3D");
            rawGo.transform.SetParent(slot.transform, false);
            _characterRawImage                = rawGo.AddComponent<RawImage>();
            _characterRawImage.raycastTarget  = false;
            _characterRawImage.color          = Color.white;
            _characterRawImage.gameObject.SetActive(false);
            var rawRt = rawGo.GetComponent<RectTransform>();
            rawRt.anchorMin = Vector2.zero;
            rawRt.anchorMax = Vector2.one;
            rawRt.offsetMin = rawRt.offsetMax = Vector2.zero;

            // Placeholder — dark tinted background + initial letter
            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(slot.transform, false);
            _placeholderBg              = phGo.AddComponent<Image>();
            _placeholderBg.raycastTarget = false;
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = phRt.offsetMax = Vector2.zero;

            var initGo  = new GameObject("Initial");
            initGo.transform.SetParent(phGo.transform, false);
            _placeholderInitial                = initGo.AddComponent<TextMeshProUGUI>();
            _placeholderInitial.alignment      = TextAlignmentOptions.Center;
            _placeholderInitial.fontStyle      = FontStyles.Bold;
            _placeholderInitial.enableAutoSizing = true;
            _placeholderInitial.fontSizeMin    = 24;
            _placeholderInitial.fontSizeMax    = 120;
            _placeholderInitial.raycastTarget  = false;
            var initRt = initGo.GetComponent<RectTransform>();
            initRt.anchorMin = new Vector2(0.1f, 0.2f);
            initRt.anchorMax = new Vector2(0.9f, 0.8f);
            initRt.offsetMin = initRt.offsetMax = Vector2.zero;
        }

        private TextMeshProUGUI FindTMP(string childName)
        {
            var t = transform.Find(childName);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        // ── Character portrait ─────────────────────────────────────────────────
        private Transform CharacterSlot => _characterImage?.transform.parent;

        /// <summary>Show a real sprite portrait.</summary>
        public void SetCharacterSprite(Sprite sprite)
        {
            if (_characterImage == null) return;
            _characterImage.sprite = sprite;
            _characterImage.gameObject.SetActive(true);
            if (_characterRawImage != null) _characterRawImage.gameObject.SetActive(false);
            if (_placeholderBg != null) _placeholderBg.gameObject.SetActive(false);
            ShowSlot(true);
        }

        /// <summary>Show a 3D Render Texture inside the character slot.</summary>
        public void SetCharacterRenderTexture(RenderTexture rt)
        {
            if (_characterRawImage == null) return;
            _characterRawImage.texture = rt;
            _characterRawImage.gameObject.SetActive(true);
            if (_characterImage != null) _characterImage.gameObject.SetActive(false);
            if (_placeholderBg != null) _placeholderBg.gameObject.SetActive(false);
            ShowSlot(true);
        }

        /// <summary>Show a coloured placeholder with the character's initial letter.</summary>
        public void ShowCharacterPlaceholder(string name, Color accentColor)
        {
            if (_placeholderBg == null) return;
            _characterImage.gameObject.SetActive(false);
            if (_characterRawImage != null) _characterRawImage.gameObject.SetActive(false);

            _placeholderBg.color = new Color(
                accentColor.r * 0.18f, accentColor.g * 0.18f, accentColor.b * 0.18f, 0.95f);
            _placeholderBg.gameObject.SetActive(true);

            if (_placeholderInitial != null)
            {
                _placeholderInitial.text  = string.IsNullOrEmpty(name) ? "?" : name[..1].ToUpper();
                _placeholderInitial.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.85f);
            }
            ShowSlot(true);
        }

        /// <summary>Hide the entire character slot (portrait + placeholder).</summary>
        public void ShowCharacterImage(bool show)
        {
            ShowSlot(show);
            if (!show)
            {
                if (_characterImage  != null) _characterImage.gameObject.SetActive(false);
                if (_characterRawImage != null) _characterRawImage.gameObject.SetActive(false);
                if (_placeholderBg   != null) _placeholderBg.gameObject.SetActive(false);
            }
        }

        private void ShowSlot(bool show)
        {
            var slot = CharacterSlot;
            if (slot != null) slot.gameObject.SetActive(show);
            if (_dialogueRt == null) return;
            _dialogueRt.anchorMin = show ? TextAnchorMinWithChar : TextAnchorMinFull;
            _dialogueRt.anchorMax = show ? TextAnchorMaxWithChar : TextAnchorMaxFull;
        }

        // ── Dialogue ───────────────────────────────────────────────────────────
        public void SetDialogueText(string text)
        {
            if (_dialogueText != null)
            {
                _dialogueText.gameObject.SetActive(true);
                _dialogueText.text = text;
            }
        }

        public void AnimateLineIn()
        {
            if (_textAnimRoutine != null) StopCoroutine(_textAnimRoutine);
            _textAnimRoutine = StartCoroutine(LineEntranceRoutine());
        }

        private IEnumerator LineEntranceRoutine()
        {
            if (_panelCg == null) yield break;
            var rt = GetComponent<RectTransform>();
            var basePos = rt != null ? rt.anchoredPosition : Vector2.zero;
            if (rt != null) rt.anchoredPosition = basePos + new Vector2(0f, -6f);
            _panelCg.alpha = 0.4f;

            float e = 0f, dur = 0.22f;
            while (e < dur)
            {
                e += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, e / dur);
                _panelCg.alpha = Mathf.Lerp(0.4f, 1f, t);
                if (rt != null) rt.anchoredPosition = Vector2.Lerp(basePos + new Vector2(0f, -6f), basePos, t);
                yield return null;
            }
            _panelCg.alpha = 1f;
            if (rt != null) rt.anchoredPosition = basePos;
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

        // No-ops kept for compatibility
        public void SetSpeakerName(string name) { }
        public void SetPortrait(string portraitKey) { }

        // ── Choices ────────────────────────────────────────────────────────────
        public void ShowChoices(List<Choice> choices, Action<int> onChoiceSelected)
        {
            HideChoices();

            // Hide dialogue text — choices take the right side
            if (_dialogueText != null) _dialogueText.gameObject.SetActive(false);

            _choicesContainer.gameObject.SetActive(true);

            for (int i = 0; i < choices.Count; i++)
            {
                string choiceText = choices[i].text;

                if (choiceText.StartsWith("input:"))
                {
                    string varName = choiceText.Substring("input:".Length);
                    BuildTextInputChoice(i, varName, onChoiceSelected);
                }
                else
                {
                    var btn = Instantiate(_choiceButtonPrefab, _choicesContainer);

                    var le = btn.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                    le.preferredHeight = 62;
                    le.minHeight       = 48;

                    btn.Setup(i, choiceText, onChoiceSelected);
                    btn.AnimateIn(i);
                    _choiceButtons.Add(btn);
                }
            }
        }

        public void HideChoices()
        {
            _choiceButtons.Clear();

            if (_choicesContainer != null)
            {
                for (int i = _choicesContainer.childCount - 1; i >= 0; i--)
                    Destroy(_choicesContainer.GetChild(i).gameObject);
                _choicesContainer.gameObject.SetActive(false);
            }

            if (_dialogueText != null) _dialogueText.gameObject.SetActive(true);
        }

        private void BuildTextInputChoice(int index, string varName, Action<int> onChoiceSelected)
        {
            // Container
            var container = new GameObject("NameInputChoice");
            container.transform.SetParent(_choicesContainer, false);
            var le = container.AddComponent<LayoutElement>();
            le.preferredHeight = 90;
            le.minHeight = 70;
            container.AddComponent<RectTransform>();
            var containerBg = container.AddComponent<Image>();
            containerBg.color = new Color(0.10f, 0.10f, 0.16f, 0.92f);

            // Input field background
            var inputGo = new GameObject("InputField");
            inputGo.transform.SetParent(container.transform, false);
            var inputRt = inputGo.AddComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0.02f, 0.12f);
            inputRt.anchorMax = new Vector2(0.72f, 0.88f);
            inputRt.offsetMin = inputRt.offsetMax = Vector2.zero;
            var inputBg = inputGo.AddComponent<Image>();
            inputBg.color = new Color(0.06f, 0.06f, 0.10f, 0.95f);
            var inputField = inputGo.AddComponent<TMP_InputField>();

            // Viewport
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(inputGo.transform, false);
            var viewportRt = viewportGo.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(8, 2);
            viewportRt.offsetMax = new Vector2(-8, -2);
            viewportGo.AddComponent<RectMask2D>();

            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(viewportGo.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            textTmp.color = Color.white;
            textTmp.fontSize = 18;
            textTmp.raycastTarget = false;

            // Placeholder
            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(viewportGo.transform, false);
            var phRt = phGo.AddComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = phRt.offsetMax = Vector2.zero;
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            phTmp.color = new Color(0.55f, 0.55f, 0.65f, 0.8f);
            phTmp.fontSize = 18;
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.text = "Enter your name...";
            phTmp.raycastTarget = false;

            // Wire TMP_InputField
            inputField.textViewport = viewportRt;
            inputField.textComponent = textTmp;
            inputField.placeholder = phTmp;
            inputField.characterLimit = 32;
            inputField.ActivateInputField();

            // Confirm button
            var confirmGo = new GameObject("ConfirmButton");
            confirmGo.transform.SetParent(container.transform, false);
            var confirmRt = confirmGo.AddComponent<RectTransform>();
            confirmRt.anchorMin = new Vector2(0.74f, 0.12f);
            confirmRt.anchorMax = new Vector2(0.98f, 0.88f);
            confirmRt.offsetMin = confirmRt.offsetMax = Vector2.zero;
            var confirmImg = confirmGo.AddComponent<Image>();
            confirmImg.color = new Color(0.22f, 0.42f, 0.78f, 0.95f);
            var confirmBtn = confirmGo.AddComponent<Button>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(confirmGo.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "Confirm";
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.fontSize = 16;
            labelTmp.color = Color.white;
            labelTmp.raycastTarget = false;

            void Confirm()
            {
                string entered = inputField.text.Trim();
                if (!string.IsNullOrEmpty(entered) && varName == "player_name")
                {
                    var save = GameStateManager.Instance?.SaveData;
                    if (save != null) save.PlayerName = entered;
                }
                onChoiceSelected(index);
            }

            confirmBtn.onClick.AddListener(Confirm);
            inputField.onSubmit.AddListener(_ => Confirm());
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
