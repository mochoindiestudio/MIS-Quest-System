using MochoIndieStudio.QuestSystem;
using TMPro;
using UnityEngine;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// The HUD coordinator: routes the panel toggle keys, owns the interaction-prompt label, and
    /// turns quest-system events into quest-log refreshes and toasts. Sits on the same GameObject as
    /// <see cref="DemoGame"/> so its <c>OnEnable</c> runs after <see cref="DemoGame"/>'s <c>Awake</c>
    /// (the <see cref="QuestLog"/> already exists) and before the quest list is registered.
    /// </summary>
    public sealed class DemoHud : MonoBehaviour
    {
        [SerializeField]
        private DemoInput input;

        [SerializeField]
        private DemoGame game;

        [SerializeField]
        private QuestLogView questLog;

        [SerializeField]
        private InventoryView inventory;

        [SerializeField]
        private PauseMenuView pauseMenu;

        [SerializeField]
        private GameObject helpPanel;

        [SerializeField]
        private ToastView toast;

        [SerializeField]
        private CanvasGroup promptGroup;

        [SerializeField]
        private TMP_Text promptLabel;

        private bool questEventsHooked;

        private void OnEnable()
        {
            if (input != null)
            {
                input.QuestLogPressed += OnToggleQuestLog;
                input.InventoryPressed += OnToggleInventory;
                input.MenuPressed += OnToggleMenu;
                input.HelpPressed += OnToggleHelp;
            }

            HookQuestEvents();
            HidePrompt();
        }

        private void Start()
        {
            // Fallback in case DemoGame lives on another GameObject and its Awake ran late.
            HookQuestEvents();
            questLog?.Refresh();
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.QuestLogPressed -= OnToggleQuestLog;
                input.InventoryPressed -= OnToggleInventory;
                input.MenuPressed -= OnToggleMenu;
                input.HelpPressed -= OnToggleHelp;
            }

            if (questEventsHooked && game != null && game.Log != null)
            {
                game.Log.OnObjectiveActivated -= OnObjectiveActivated;
                game.Log.OnObjectiveCompleted -= OnObjectiveChanged;
                game.Log.OnQuestAdvanced -= OnObjectiveChanged;
                game.Log.OnQuestStarted -= OnQuestStarted;
                game.Log.OnQuestCompleted -= OnQuestCompleted;
                questEventsHooked = false;
            }
        }

        private void HookQuestEvents()
        {
            if (questEventsHooked || game == null || game.Log == null)
            {
                return;
            }

            game.Log.OnObjectiveActivated += OnObjectiveActivated;
            game.Log.OnObjectiveCompleted += OnObjectiveChanged;
            game.Log.OnQuestAdvanced += OnObjectiveChanged;
            game.Log.OnQuestStarted += OnQuestStarted;
            game.Log.OnQuestCompleted += OnQuestCompleted;
            questEventsHooked = true;
        }

        private void OnToggleQuestLog()
        {
            if (pauseMenu == null || !pauseMenu.IsOpen)
            {
                questLog?.Toggle();
            }
        }

        private void OnToggleInventory()
        {
            if (pauseMenu == null || !pauseMenu.IsOpen)
            {
                inventory?.Toggle();
            }
        }

        private void OnToggleMenu() => pauseMenu?.Toggle();

        private void OnToggleHelp()
        {
            if (helpPanel != null)
            {
                helpPanel.SetActive(!helpPanel.activeSelf);
            }
        }

        private void OnObjectiveActivated(QuestHandle quest, ObjectiveHandle objective)
        {
            toast?.Show("New objective available! Check your Quest Book.", 3f);
            questLog?.Refresh();
        }

        private void OnObjectiveChanged(QuestHandle quest, ObjectiveHandle objective) => questLog?.Refresh();

        private void OnQuestStarted(QuestHandle quest) => questLog?.Refresh();

        private void OnQuestCompleted(QuestHandle quest)
        {
            toast?.Show($"Quest complete: {quest.Title}!", 3f);
            questLog?.Refresh();
        }

        /// <summary>Shows the interaction hint (called by <see cref="PlayerController"/>).</summary>
        public void ShowPrompt(string text)
        {
            if (promptLabel != null)
            {
                promptLabel.text = text;
            }

            if (promptGroup != null)
            {
                promptGroup.alpha = 1f;
            }
        }

        /// <summary>Hides the interaction hint.</summary>
        public void HidePrompt()
        {
            if (promptGroup != null)
            {
                promptGroup.alpha = 0f;
            }
        }
    }
}
