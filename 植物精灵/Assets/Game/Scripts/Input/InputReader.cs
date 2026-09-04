using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlantSpirit.GGJ
{
    // The only gameplay input entry point. Consumers receive semantic events, never raw keys.
    public sealed class InputReader : MonoBehaviour
    {
        public event Action<Vector2> Move;
        public event Action Jump;
        public event Action Dash;
        public event Action Attack;
        public event Action Skill;
        public event Action Interact;
        public event Action Graft;
        public event Action Pause;
        public event Action<int> GraftSelect;

        private InputActionMap gameplay;
        private InputAction move;
        private InputAction jump;
        private InputAction dash;
        private InputAction attack;
        private InputAction skill;
        private InputAction interact;
        private InputAction graft;
        private InputAction pause;
        private InputAction selectRoot;
        private InputAction selectStem;
        private InputAction selectFlower;
        private float gameplayBlockedUntil;
        private bool gameplayLocked;

        public void BlockGameplayFor(float seconds) => gameplayBlockedUntil = Mathf.Max(gameplayBlockedUntil, Time.unscaledTime + seconds);
        public void SetGameplayLocked(bool locked)
        {
            gameplayLocked = locked;
            if (locked) Move?.Invoke(Vector2.zero);
        }
        public void RequestPause() => Pause?.Invoke();

        private void Awake()
        {
            gameplay = new InputActionMap("Gameplay");
            move = gameplay.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            dash = Button("Dash", "<Keyboard>/leftShift", "<Gamepad>/rightShoulder");
            attack = Button("Attack", "<Mouse>/leftButton", "<Keyboard>/j");
            skill = Button("Skill", "<Mouse>/rightButton", "<Keyboard>/k");
            interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonWest");
            graft = Button("Graft", "<Keyboard>/tab", "<Keyboard>/g");
            pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
            selectRoot = Button("SelectRoot", "<Keyboard>/1");
            selectStem = Button("SelectStem", "<Keyboard>/2");
            selectFlower = Button("SelectFlower", "<Keyboard>/3");
        }

        private InputAction Button(string name, string primary, string secondary = null)
        {
            InputAction action = gameplay.AddAction(name, InputActionType.Button);
            action.AddBinding(primary);
            if (!string.IsNullOrEmpty(secondary)) action.AddBinding(secondary);
            return action;
        }

        private void OnEnable() => gameplay?.Enable();
        private void OnDisable() => gameplay?.Disable();
        private void OnDestroy() => gameplay?.Dispose();

        private void Update()
        {
            GameState state = GameBootstrap.Instance == null ? GameState.MainMenu : GameBootstrap.Instance.State.Current;
            if (gameplayLocked) return;
            if (pause.WasPressedThisFrame() && (state == GameState.Playing || state == GameState.Grafting || state == GameState.Paused))
            {
                Pause?.Invoke();
                return;
            }
            if (state == GameState.Grafting)
            {
                if (graft.WasPressedThisFrame()) Graft?.Invoke();
                if (selectRoot.WasPressedThisFrame()) GraftSelect?.Invoke(0);
                if (selectStem.WasPressedThisFrame()) GraftSelect?.Invoke(1);
                if (selectFlower.WasPressedThisFrame()) GraftSelect?.Invoke(2);
                return;
            }
            if (state != GameState.Playing) return;
            Move?.Invoke(move.ReadValue<Vector2>());
            if (Time.unscaledTime < gameplayBlockedUntil) return;
            if (jump.WasPressedThisFrame()) Jump?.Invoke();
            if (dash.WasPressedThisFrame()) Dash?.Invoke();
            if (attack.WasPressedThisFrame()) Attack?.Invoke();
            if (skill.WasPressedThisFrame()) Skill?.Invoke();
            if (interact.WasPressedThisFrame()) Interact?.Invoke();
            if (graft.WasPressedThisFrame()) Graft?.Invoke();
        }
    }
}
