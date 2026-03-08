using NGames.Core.State;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NGames.UI
{
    /// <summary>Wired up by AutoSetup in the MainMenu scene.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _newGameBtn;
        [SerializeField] private Button _continueBtn;
        [SerializeField] private Button _quitBtn;

        private void Start()
        {
            bool hasSave = false;
            for (int i = 0; i < SaveSystem.SlotCount; i++)
                if (SaveSystem.SlotExists(i)) { hasSave = true; break; }

            if (_continueBtn != null)
                _continueBtn.gameObject.SetActive(hasSave);

            _newGameBtn?.onClick.AddListener(OnNewGame);
            _continueBtn?.onClick.AddListener(OnContinue);
            _quitBtn?.onClick.AddListener(OnQuit);
        }

        private void OnNewGame()
        {
            SaveSystem.PendingLoadSlot = -1;
            SceneManager.LoadScene("Bootstrap");
        }

        private void OnContinue()
        {
            // Find most recent slot
            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                if (SaveSystem.SlotExists(i))
                {
                    SaveSystem.PendingLoadSlot = i;
                    break;
                }
            }
            SceneManager.LoadScene("Bootstrap");
        }

        private static void OnQuit() => Application.Quit();
    }
}
