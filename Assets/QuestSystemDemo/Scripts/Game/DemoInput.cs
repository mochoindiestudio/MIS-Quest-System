using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>
    /// All demo input in one place, built from code <see cref="InputAction"/>s (the project is new
    /// Input System only). Movement is polled via <see cref="Move"/>; the button actions fire events.
    /// </summary>
    public sealed class DemoInput : MonoBehaviour
    {
        private InputAction move;
        private InputAction interact;
        private InputAction questLog;
        private InputAction inventory;
        private InputAction menu;
        private InputAction help;

        /// <summary>Current movement vector (WASD or arrows), or zero before the actions are built.</summary>
        public Vector2 Move => move != null ? move.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>E pressed.</summary>
        public event Action InteractPressed;

        /// <summary>Tab pressed.</summary>
        public event Action QuestLogPressed;

        /// <summary>I pressed.</summary>
        public event Action InventoryPressed;

        /// <summary>Escape pressed.</summary>
        public event Action MenuPressed;

        /// <summary>F1 pressed.</summary>
        public event Action HelpPressed;

        private void OnEnable()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            questLog = new InputAction("QuestLog", InputActionType.Button, "<Keyboard>/tab");
            inventory = new InputAction("Inventory", InputActionType.Button, "<Keyboard>/i");
            menu = new InputAction("Menu", InputActionType.Button, "<Keyboard>/escape");
            help = new InputAction("Help", InputActionType.Button, "<Keyboard>/f1");

            interact.performed += _ => InteractPressed?.Invoke();
            questLog.performed += _ => QuestLogPressed?.Invoke();
            inventory.performed += _ => InventoryPressed?.Invoke();
            menu.performed += _ => MenuPressed?.Invoke();
            help.performed += _ => HelpPressed?.Invoke();

            move.Enable();
            interact.Enable();
            questLog.Enable();
            inventory.Enable();
            menu.Enable();
            help.Enable();
        }

        private void OnDisable()
        {
            move?.Dispose();
            interact?.Dispose();
            questLog?.Dispose();
            inventory?.Dispose();
            menu?.Dispose();
            help?.Dispose();

            move = null;
            interact = null;
            questLog = null;
            inventory = null;
            menu = null;
            help = null;
        }
    }
}
