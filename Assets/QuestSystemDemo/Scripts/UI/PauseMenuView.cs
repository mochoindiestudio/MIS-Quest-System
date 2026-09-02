using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// The ESC menu. Opening it freezes the game (<c>Time.timeScale = 0</c>) and shows Restart / Quit.
    /// </summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField]
        private GameObject panel;

        [SerializeField]
        private Button restartButton;

        [SerializeField]
        private Button quitButton;

        /// <summary>Whether the menu is currently shown.</summary>
        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake() => SetOpen(false);

        private void Start()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(Restart);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(Quit);
            }
        }

        private void OnDisable()
        {
            // Never leave the game frozen if this object goes away while open.
            Time.timeScale = 1f;
        }

        /// <summary>Flips the menu open/closed.</summary>
        public void Toggle() => SetOpen(!IsOpen);

        /// <summary>Shows or hides the menu and freezes / unfreezes the game.</summary>
        public void SetOpen(bool open)
        {
            if (panel != null)
            {
                panel.SetActive(open);
            }

            Time.timeScale = open ? 0f : 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Quit()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
