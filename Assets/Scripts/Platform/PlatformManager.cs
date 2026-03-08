using UnityEngine;

namespace NGames.Platform
{
    /// <summary>
    /// Centralizes platform detection and platform-specific behaviour.
    ///
    /// Web (WebGL):
    ///   - Save via PlayerPrefs (backed by IndexedDB in browser)
    ///   - Screen resolution locked to canvas size
    ///   - No native file system access
    ///
    /// Mobile (iOS / Android):
    ///   - Save via Application.persistentDataPath JSON files
    ///   - Addressables for hot episode delivery (avoids weekly App Store submissions)
    ///   - Back-button handling on Android
    ///
    /// Desktop (Editor / Standalone):
    ///   - Save via Application.persistentDataPath
    ///   - Full keyboard input
    /// </summary>
    public class PlatformManager : MonoBehaviour
    {
        public static PlatformManager Instance { get; private set; }

        public enum Platform { Web, Mobile, Desktop }

        public Platform CurrentPlatform { get; private set; }

        public bool IsWeb     => CurrentPlatform == Platform.Web;
        public bool IsMobile  => CurrentPlatform == Platform.Mobile;
        public bool IsDesktop => CurrentPlatform == Platform.Desktop;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DetectPlatform();
            ApplyPlatformSettings();
        }

        private void DetectPlatform()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CurrentPlatform = Platform.Web;
#elif (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            CurrentPlatform = Platform.Mobile;
#else
            CurrentPlatform = Platform.Desktop;
#endif
            Debug.Log($"[PlatformManager] Detected platform: {CurrentPlatform}");
        }

        private void ApplyPlatformSettings()
        {
            switch (CurrentPlatform)
            {
                case Platform.Web:
                    Application.targetFrameRate = 60;
                    Screen.sleepTimeout = SleepTimeout.NeverSleep;
                    break;

                case Platform.Mobile:
                    Application.targetFrameRate = 60;
                    Screen.sleepTimeout = SleepTimeout.NeverSleep;
                    Input.multiTouchEnabled = true;
                    break;

                case Platform.Desktop:
                    Application.targetFrameRate = 60;
                    break;
            }
        }

        private void Update()
        {
            // Android back button — return to episode select
            if (IsMobile && Input.GetKeyDown(KeyCode.Escape))
                HandleAndroidBack();
        }

        private void HandleAndroidBack()
        {
            // Publish an event that the episode select screen can handle
            Core.Events.GameEventBus.Publish(new AndroidBackPressedEvent());
        }
    }

    public class AndroidBackPressedEvent { }
}
