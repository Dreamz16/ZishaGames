using System;
using System.Collections;
using NGames.Core.State;
using NGames.Episodes;
using NGames.Core.Narrative;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NGames.UI
{
    /// <summary>
    /// Self-bootstrapping login / register / welcome screen.
    ///
    /// Shown exactly once per app session (until the user authenticates).
    /// After authentication it either:
    ///   - Loads "Bootstrap" with a resume slot prepared (Continue), or
    ///   - Loads "Bootstrap" fresh (New Game).
    ///
    /// The component is DontDestroyOnLoad so it survives the Bootstrap scene load
    /// and can hand off gracefully after authentication.
    /// </summary>
    public class LoginViewController : MonoBehaviour
    {
        // ── Session guard ──────────────────────────────────────────────────────
        private static bool _shown;   // reset by calling AccountSystem.Logout()

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Suppress if the user has already authenticated this session
            if (AccountSystem.IsAuthenticated) return;
            if (_shown) return;
            if (FindFirstObjectByType<LoginViewController>() != null) return;

            _shown = true;
            var go = new GameObject("[LoginViewController]");
            DontDestroyOnLoad(go);
            go.AddComponent<LoginViewController>();
        }

        // ── Panels ─────────────────────────────────────────────────────────────
        private enum Screen { Login, Register, Welcome }
        private Screen _screen = Screen.Login;

        // Login view
        private TMP_InputField _loginUser;
        private TMP_InputField _loginPass;
        private TextMeshProUGUI _loginError;

        // Register view
        private TMP_InputField _regUser;
        private TMP_InputField _regPass;
        private TMP_InputField _regConfirm;
        private TextMeshProUGUI _regError;

        // Welcome view
        private TextMeshProUGUI _welcomeLabel;
        private GameObject      _saveCard;
        private TextMeshProUGUI _saveSceneLabel;
        private TextMeshProUGUI _saveTimeLabel;
        private Button          _continueBtn;

        // View roots
        private GameObject _loginRoot;
        private GameObject _registerRoot;
        private GameObject _welcomeRoot;

        // Root canvas
        private Canvas _canvas;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            EnsureEventSystem();
            BuildUI();
            ShowScreen(Screen.Login);
        }

        // ── UI construction ────────────────────────────────────────────────────
        private void BuildUI()
        {
            // Full-screen dark overlay canvas
            var cgo    = new GameObject("LoginCanvas");
            cgo.transform.SetParent(transform, false);
            _canvas                    = cgo.AddComponent<Canvas>();
            _canvas.renderMode         = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder       = 300;
            var scaler                 = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            cgo.AddComponent<GraphicRaycaster>();

            // Semi-transparent overlay
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(cgo.transform, false);
            overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
            var ovRt = overlay.GetComponent<RectTransform>();
            ovRt.anchorMin = Vector2.zero;
            ovRt.anchorMax = Vector2.one;
            ovRt.offsetMin = ovRt.offsetMax = Vector2.zero;

            // Card — centered, 460×560
            var card = new GameObject("Card");
            card.transform.SetParent(cgo.transform, false);
            var cardImg   = card.AddComponent<Image>();
            cardImg.color = new Color(0.06f, 0.06f, 0.10f, 1f);
            var cardRt    = card.GetComponent<RectTransform>();
            cardRt.anchorMin        = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax        = new Vector2(0.5f, 0.5f);
            cardRt.pivot            = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta        = new Vector2(460f, 560f);

            // Top accent bar
            var accentGo  = new GameObject("TopAccent");
            accentGo.transform.SetParent(card.transform, false);
            accentGo.AddComponent<Image>().color = new Color(0.50f, 0.35f, 0.90f, 1f);
            var accentRt  = accentGo.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 1f);
            accentRt.anchorMax = Vector2.one;
            accentRt.offsetMin = new Vector2(0f, -4f);
            accentRt.offsetMax = Vector2.zero;

            // Game title
            var titleGo  = new GameObject("Title");
            titleGo.transform.SetParent(card.transform, false);
            var titleTmp  = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text      = "NGAMES";
            titleTmp.fontSize  = 32;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color     = new Color(0.90f, 0.85f, 1.00f, 1f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            var titleRt   = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin        = new Vector2(0f, 1f);
            titleRt.anchorMax        = new Vector2(1f, 1f);
            titleRt.pivot            = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -20f);
            titleRt.sizeDelta        = new Vector2(0f, 46f);

            // Sub-title bar (changes per screen)
            var subGo  = new GameObject("SubTitle");
            subGo.transform.SetParent(card.transform, false);
            var subTmp  = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.fontSize  = 13;
            subTmp.fontStyle = FontStyles.Italic;
            subTmp.color     = new Color(0.60f, 0.55f, 0.75f, 1f);
            subTmp.alignment = TextAlignmentOptions.Center;
            var subRt   = subGo.GetComponent<RectTransform>();
            subRt.anchorMin        = new Vector2(0f, 1f);
            subRt.anchorMax        = new Vector2(1f, 1f);
            subRt.pivot            = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -72f);
            subRt.sizeDelta        = new Vector2(0f, 22f);

            // ── Build each view inside the card ────────────────────────────────
            _loginRoot    = BuildLoginView(card.transform);
            _registerRoot = BuildRegisterView(card.transform);
            _welcomeRoot  = BuildWelcomeView(card.transform);
        }

        // ── Login View ─────────────────────────────────────────────────────────
        private GameObject BuildLoginView(Transform parent)
        {
            var root = new GameObject("LoginView");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 10f);
            rt.sizeDelta        = new Vector2(380f, 400f);

            float y = 130f;

            var usernameLabel = MakeLabel(root.transform, "Username", y);
            y -= 26f;
            _loginUser = MakeInputField(root.transform, "Enter username", false, y);
            y -= 60f;

            var passwordLabel = MakeLabel(root.transform, "Password", y);
            y -= 26f;
            _loginPass = MakeInputField(root.transform, "Enter password", true, y);
            y -= 72f;

            var loginBtn = MakeButton(root.transform, "SIGN IN",
                new Color(0.45f, 0.28f, 0.85f, 1f), y, 48f);
            loginBtn.onClick.AddListener(OnLoginClicked);
            y -= 64f;

            var sep = MakeSeparator(root.transform, y);
            y -= 28f;

            var regBtn = MakeTextButton(root.transform, "Don't have an account?  Register", y);
            regBtn.onClick.AddListener(() => ShowScreen(Screen.Register));
            y -= 36f;

            _loginError = MakeErrorLabel(root.transform, y);

            return root;
        }

        // ── Register View ──────────────────────────────────────────────────────
        private GameObject BuildRegisterView(Transform parent)
        {
            var root = new GameObject("RegisterView");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta        = new Vector2(380f, 440f);

            float y = 170f;

            MakeLabel(root.transform, "Username", y);
            y -= 26f;
            _regUser = MakeInputField(root.transform, "Choose a username (min 3 chars)", false, y);
            y -= 60f;

            MakeLabel(root.transform, "Password", y);
            y -= 26f;
            _regPass = MakeInputField(root.transform, "Choose a password (min 4 chars)", true, y);
            y -= 60f;

            MakeLabel(root.transform, "Confirm Password", y);
            y -= 26f;
            _regConfirm = MakeInputField(root.transform, "Repeat password", true, y);
            y -= 72f;

            var createBtn = MakeButton(root.transform, "CREATE ACCOUNT",
                new Color(0.25f, 0.58f, 0.45f, 1f), y, 48f);
            createBtn.onClick.AddListener(OnRegisterClicked);
            y -= 64f;

            var backBtn = MakeTextButton(root.transform, "← Back to sign in", y);
            backBtn.onClick.AddListener(() => ShowScreen(Screen.Login));
            y -= 36f;

            _regError = MakeErrorLabel(root.transform, y);

            return root;
        }

        // ── Welcome View ───────────────────────────────────────────────────────
        private GameObject BuildWelcomeView(Transform parent)
        {
            var root = new GameObject("WelcomeView");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 10f);
            rt.sizeDelta        = new Vector2(380f, 380f);

            // Welcome label
            var wGo  = new GameObject("WelcomeLabel");
            wGo.transform.SetParent(root.transform, false);
            _welcomeLabel           = wGo.AddComponent<TextMeshProUGUI>();
            _welcomeLabel.fontSize  = 20;
            _welcomeLabel.fontStyle = FontStyles.Bold;
            _welcomeLabel.color     = new Color(0.90f, 0.85f, 1.00f, 1f);
            _welcomeLabel.alignment = TextAlignmentOptions.Center;
            var wRt                 = wGo.GetComponent<RectTransform>();
            wRt.anchorMin           = new Vector2(0f, 1f);
            wRt.anchorMax           = new Vector2(1f, 1f);
            wRt.pivot               = new Vector2(0.5f, 1f);
            wRt.anchoredPosition    = new Vector2(0f, -16f);
            wRt.sizeDelta           = new Vector2(0f, 32f);

            // Save card (dark inset card)
            _saveCard = new GameObject("SaveCard");
            _saveCard.transform.SetParent(root.transform, false);
            var scImg = _saveCard.AddComponent<Image>();
            scImg.color = new Color(0.10f, 0.10f, 0.16f, 1f);
            var scRt = _saveCard.GetComponent<RectTransform>();
            scRt.anchorMin        = new Vector2(0.5f, 1f);
            scRt.anchorMax        = new Vector2(0.5f, 1f);
            scRt.pivot            = new Vector2(0.5f, 1f);
            scRt.anchoredPosition = new Vector2(0f, -68f);
            scRt.sizeDelta        = new Vector2(340f, 130f);

            // Left accent on save card
            var scAccent = new GameObject("SaveCardAccent");
            scAccent.transform.SetParent(_saveCard.transform, false);
            scAccent.AddComponent<Image>().color = new Color(0.45f, 0.28f, 0.85f, 1f);
            var scaRt = scAccent.GetComponent<RectTransform>();
            scaRt.anchorMin = Vector2.zero;
            scaRt.anchorMax = new Vector2(0f, 1f);
            scaRt.offsetMin = Vector2.zero;
            scaRt.offsetMax = new Vector2(4f, 0f);

            // Scene name inside save card
            var snGo = new GameObject("SceneLabel");
            snGo.transform.SetParent(_saveCard.transform, false);
            _saveSceneLabel           = snGo.AddComponent<TextMeshProUGUI>();
            _saveSceneLabel.fontSize  = 15;
            _saveSceneLabel.fontStyle = FontStyles.Bold;
            _saveSceneLabel.color     = new Color(0.88f, 0.88f, 0.96f, 1f);
            _saveSceneLabel.alignment = TextAlignmentOptions.TopLeft;
            var snRt                  = snGo.GetComponent<RectTransform>();
            snRt.anchorMin            = Vector2.zero;
            snRt.anchorMax            = Vector2.one;
            snRt.offsetMin            = new Vector2(16f, 40f);
            snRt.offsetMax            = new Vector2(-12f, -12f);

            // Timestamp inside save card
            var tsGo = new GameObject("TimeLabel");
            tsGo.transform.SetParent(_saveCard.transform, false);
            _saveTimeLabel           = tsGo.AddComponent<TextMeshProUGUI>();
            _saveTimeLabel.fontSize  = 12;
            _saveTimeLabel.color     = new Color(0.55f, 0.55f, 0.65f, 1f);
            _saveTimeLabel.fontStyle = FontStyles.Italic;
            _saveTimeLabel.alignment = TextAlignmentOptions.BottomLeft;
            var tsRt                 = tsGo.GetComponent<RectTransform>();
            tsRt.anchorMin           = Vector2.zero;
            tsRt.anchorMax           = Vector2.one;
            tsRt.offsetMin           = new Vector2(16f, 8f);
            tsRt.offsetMax           = new Vector2(-12f, -50f);

            // Continue button inside save card (anchored to bottom of card)
            _continueBtn = MakeButton(_saveCard.transform, "▶  CONTINUE",
                new Color(0.45f, 0.28f, 0.85f, 1f), -100f, 36f, anchoredToBottom: true);
            _continueBtn.onClick.AddListener(OnContinueClicked);

            // New Game button (below card)
            var ngBtn = MakeButton(root.transform, "NEW GAME",
                new Color(0.18f, 0.18f, 0.25f, 1f), -230f, 44f);
            ngBtn.onClick.AddListener(OnNewGameClicked);

            // Logout link
            var logoutBtn = MakeTextButton(root.transform, "Sign out", -290f);
            logoutBtn.onClick.AddListener(OnLogoutClicked);

            return root;
        }

        // ── Screen switching ───────────────────────────────────────────────────
        private void ShowScreen(Screen s)
        {
            _screen = s;
            _loginRoot.SetActive(s == Screen.Login);
            _registerRoot.SetActive(s == Screen.Register);
            _welcomeRoot.SetActive(s == Screen.Welcome);

            if (s == Screen.Welcome) PopulateWelcomeScreen();
        }

        private void PopulateWelcomeScreen()
        {
            var user = AccountSystem.CurrentUser;
            _welcomeLabel.text = $"Welcome back, <b>{user}</b>!";

            var slot = AccountSystem.GetSave();
            if (slot != null)
            {
                _saveCard.SetActive(true);
                _saveSceneLabel.text = string.IsNullOrEmpty(slot.SceneDescription)
                    ? "Last save"
                    : slot.SceneDescription;
                _saveTimeLabel.text  = FormatTimestamp(slot.Timestamp);
                _continueBtn.gameObject.SetActive(true);
            }
            else
            {
                _saveCard.SetActive(false);
            }
        }

        // ── Button handlers ────────────────────────────────────────────────────
        private void OnLoginClicked()
        {
            _loginError.text = string.Empty;
            if (!AccountSystem.Login(_loginUser.text, _loginPass.text, out var err))
            { _loginError.text = err; return; }

            AfterAuthentication();
        }

        private void OnRegisterClicked()
        {
            _regError.text = string.Empty;
            if (_regPass.text != _regConfirm.text)
            { _regError.text = "Passwords do not match."; return; }

            if (!AccountSystem.Register(_regUser.text, _regPass.text, out var err))
            { _regError.text = err; return; }

            AfterAuthentication();
        }

        private void AfterAuthentication()
        {
            if (AccountSystem.HasSave())
                ShowScreen(Screen.Welcome);
            else
                StartNewGame();
        }

        private void OnContinueClicked()
        {
            var slot = AccountSystem.GetSave();
            if (slot == null) { StartNewGame(); return; }
            AccountSystem.PrepareResumeViaSlot0(slot);
            SceneManager.LoadScene("Bootstrap");
            // Component is DontDestroyOnLoad — destroy once game is running
            Destroy(gameObject);
        }

        private void OnNewGameClicked()
        {
            AccountSystem.DeleteSave();
            StartNewGame();
        }

        private void OnLogoutClicked()
        {
            AccountSystem.Logout();
            _shown = false;    // allow re-show on next bootstrap call
            ShowScreen(Screen.Login);
            _loginUser.text = string.Empty;
            _loginPass.text = string.Empty;
            _loginError.text = string.Empty;
        }

        private void StartNewGame()
        {
            SaveSystem.PendingLoadSlot = -1;
            SceneManager.LoadScene("Bootstrap");
            Destroy(gameObject);
        }

        // ── UI builder helpers ─────────────────────────────────────────────────
        private static TextMeshProUGUI MakeLabel(Transform parent, string text, float topY)
        {
            var go  = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var tmp  = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text.ToUpperInvariant();
            tmp.fontSize  = 11;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = new Color(0.55f, 0.50f, 0.70f, 1f);
            tmp.alignment = TextAlignmentOptions.BottomLeft;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, topY);
            rt.sizeDelta        = new Vector2(0f, 20f);
            return tmp;
        }

        private static TMP_InputField MakeInputField(Transform parent, string placeholder, bool password, float topY)
        {
            var go  = new GameObject(password ? "PasswordField" : "UsernameField");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.16f, 1f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, topY);
            rt.sizeDelta        = new Vector2(0f, 44f);

            var input = go.AddComponent<TMP_InputField>();

            // Text viewport
            var viewportGo = new GameObject("Text Area");
            viewportGo.transform.SetParent(go.transform, false);
            viewportGo.AddComponent<RectMask2D>();
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(12f,  2f);
            viewportRt.offsetMax = new Vector2(-12f, -2f);
            input.textViewport   = viewportRt;

            // Placeholder
            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(viewportGo.transform, false);
            var ph             = phGo.AddComponent<TextMeshProUGUI>();
            ph.text            = placeholder;
            ph.fontStyle       = FontStyles.Italic;
            ph.color           = new Color(0.40f, 0.40f, 0.50f, 1f);
            ph.fontSize        = 15;
            ph.alignment       = TextAlignmentOptions.MidlineLeft;
            ph.raycastTarget   = false;
            var phRt           = phGo.GetComponent<RectTransform>();
            phRt.anchorMin     = Vector2.zero;
            phRt.anchorMax     = Vector2.one;
            phRt.offsetMin     = phRt.offsetMax = Vector2.zero;
            input.placeholder  = ph;

            // Text
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(viewportGo.transform, false);
            var txt              = txtGo.AddComponent<TextMeshProUGUI>();
            txt.color            = Color.white;
            txt.fontSize         = 15;
            txt.alignment        = TextAlignmentOptions.MidlineLeft;
            var txtRt            = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin      = Vector2.zero;
            txtRt.anchorMax      = Vector2.one;
            txtRt.offsetMin      = txtRt.offsetMax = Vector2.zero;
            input.textComponent  = txt;

            if (password)
            {
                input.inputType   = TMP_InputField.InputType.Password;
                input.asteriskChar = '\u25CF';
            }

            // Bottom border highlight
            var borderGo = new GameObject("Border");
            borderGo.transform.SetParent(go.transform, false);
            borderGo.AddComponent<Image>().color = new Color(0.45f, 0.35f, 0.70f, 0.6f);
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = new Vector2(1f, 0f);
            borderRt.offsetMin = Vector2.zero;
            borderRt.offsetMax = new Vector2(0f, 2f);

            return input;
        }

        private static Button MakeButton(Transform parent, string label, Color bgColor,
            float topY, float height, bool anchoredToBottom = false)
        {
            var go  = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var img   = go.AddComponent<Image>();
            img.color = bgColor;
            var btn   = go.AddComponent<Button>();
            var cols  = btn.colors;
            cols.normalColor      = bgColor;
            cols.highlightedColor = bgColor * 1.25f;
            cols.pressedColor     = bgColor * 0.75f;
            cols.selectedColor    = bgColor;
            btn.colors            = cols;
            var rt = go.GetComponent<RectTransform>();

            if (anchoredToBottom)
            {
                rt.anchorMin        = new Vector2(0.05f, 0f);
                rt.anchorMax        = new Vector2(0.95f, 0f);
                rt.pivot            = new Vector2(0.5f,  0f);
                rt.anchoredPosition = new Vector2(0f, 10f);
                rt.sizeDelta        = new Vector2(0f, height);
            }
            else
            {
                rt.anchorMin        = new Vector2(0f, 1f);
                rt.anchorMax        = new Vector2(1f, 1f);
                rt.pivot            = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, topY);
                rt.sizeDelta        = new Vector2(0f, height);
            }

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var tmp          = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text         = label;
            tmp.fontSize     = 14;
            tmp.fontStyle    = FontStyles.Bold;
            tmp.color        = Color.white;
            tmp.alignment    = TextAlignmentOptions.Center;
            var txtRt        = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin  = Vector2.zero;
            txtRt.anchorMax  = Vector2.one;
            txtRt.offsetMin  = txtRt.offsetMax = Vector2.zero;

            return btn;
        }

        private static Button MakeTextButton(Transform parent, string label, float topY)
        {
            var go  = new GameObject("TxtBtn_" + label);
            go.transform.SetParent(parent, false);
            var img   = go.AddComponent<Image>();
            img.color = Color.clear;
            var btn   = go.AddComponent<Button>();
            var rt    = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, topY);
            rt.sizeDelta        = new Vector2(0f, 28f);

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var tmp         = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text        = label;
            tmp.fontSize    = 13;
            tmp.color       = new Color(0.60f, 0.55f, 0.80f, 1f);
            tmp.fontStyle   = FontStyles.Underline;
            tmp.alignment   = TextAlignmentOptions.Center;
            var txtRt       = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;

            return btn;
        }

        private static GameObject MakeSeparator(Transform parent, float topY)
        {
            var go  = new GameObject("Separator");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.25f, 0.22f, 0.35f, 1f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.1f, 1f);
            rt.anchorMax        = new Vector2(0.9f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, topY);
            rt.sizeDelta        = new Vector2(0f, 1f);
            return go;
        }

        private static TextMeshProUGUI MakeErrorLabel(Transform parent, float topY)
        {
            var go  = new GameObject("ErrorLabel");
            go.transform.SetParent(parent, false);
            var tmp         = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize    = 12;
            tmp.color       = new Color(1f, 0.45f, 0.45f, 1f);
            tmp.alignment   = TextAlignmentOptions.Center;
            tmp.text        = string.Empty;
            var rt          = go.GetComponent<RectTransform>();
            rt.anchorMin    = new Vector2(0f, 1f);
            rt.anchorMax    = new Vector2(1f, 1f);
            rt.pivot        = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, topY);
            rt.sizeDelta    = new Vector2(0f, 48f);
            return tmp;
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static string FormatTimestamp(long unixSec)
        {
            try
            {
                var dt = DateTimeOffset.FromUnixTimeSeconds(unixSec).LocalDateTime;
                return $"Saved {dt:MMM d, yyyy}  ·  {dt:HH:mm}";
            }
            catch { return string.Empty; }
        }

        /// <summary>Ensure an EventSystem exists so input fields respond to clicks.</summary>
        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(esGo);
        }
    }
}
