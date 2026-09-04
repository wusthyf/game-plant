using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PlantSpirit.GGJ
{
    public sealed class GameUiController : MonoBehaviour
    {
        [SerializeField] private GameObject graftPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject deadPanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text hud;
        [SerializeField] private Text graftText;
        [SerializeField] private Text resultText;
        [SerializeField] private Text deadText;
        [SerializeField] private Text interactionText;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private GraftApplier applier;
        [SerializeField] private InputReader input;
        [SerializeField] private ExitPortal portal;
        [SerializeField] private Button rootButton;
        [SerializeField] private Button stemButton;
        [SerializeField] private Button flowerButton;
        [SerializeField] private Button closeGraftButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseMenuButton;
        [SerializeField] private Button deadRestartButton;
        [SerializeField] private Button deadMenuButton;
        [SerializeField] private Button resultRetryButton;
        [SerializeField] private Button resultMenuButton;
        private int selected;
        private bool bound;
        private Coroutine deadRevealRoutine;
        public void Configure(GameObject graft, GameObject pause, GameObject dead, GameObject result, Text hudText, Text graftPanelText, Text deadPanelText, Text resultPanelText, Text interactionPrompt, PlayerHealth playerHealth, GraftApplier graftApplier, InputReader reader, ExitPortal exitPortal)
        { Unbind(); graftPanel = graft; pausePanel = pause; deadPanel = dead; resultPanel = result; hud = hudText; graftText = graftPanelText; deadText = deadPanelText; resultText = resultPanelText; interactionText = interactionPrompt; health = playerHealth; applier = graftApplier; input = reader; portal = exitPortal; Bind(); }
        public void ConfigureButtons(Button root, Button stem, Button flower, Button closeGraft, Button resume, Button pauseMenu, Button deadRestart, Button deadMenu, Button resultRetry, Button resultMenu)
        {
            Unbind();
            rootButton = root; stemButton = stem; flowerButton = flower; closeGraftButton = closeGraft;
            resumeButton = resume; pauseMenuButton = pauseMenu; deadRestartButton = deadRestart; deadMenuButton = deadMenu;
            resultRetryButton = resultRetry; resultMenuButton = resultMenu;
            Bind();
        }
        private void OnEnable()
        {
            Bind();
        }
        private void Bind()
        {
            if (bound) return;
            if (input != null) { input.Graft += ToggleGraft; input.Pause += TogglePause; input.GraftSelect += EquipSlot; }
            if (health != null) health.Died += OnDead;
            if (GameBootstrap.Instance != null) GameBootstrap.Instance.State.Changed += OnState;
            rootButton?.onClick.AddListener(EquipRoot);
            stemButton?.onClick.AddListener(EquipStem);
            flowerButton?.onClick.AddListener(EquipFlower);
            closeGraftButton?.onClick.AddListener(CloseGraft);
            resumeButton?.onClick.AddListener(TogglePause);
            pauseMenuButton?.onClick.AddListener(Menu);
            deadRestartButton?.onClick.AddListener(Restart);
            deadMenuButton?.onClick.AddListener(Menu);
            resultRetryButton?.onClick.AddListener(Restart);
            resultMenuButton?.onClick.AddListener(Menu);
            bound = true;
        }
        private void OnDisable()
        {
            Unbind();
        }
        private void Unbind()
        {
            if (!bound) return;
            if (input != null) { input.Graft -= ToggleGraft; input.Pause -= TogglePause; input.GraftSelect -= EquipSlot; }
            if (health != null) health.Died -= OnDead;
            if (GameBootstrap.Instance != null) GameBootstrap.Instance.State.Changed -= OnState;
            rootButton?.onClick.RemoveListener(EquipRoot);
            stemButton?.onClick.RemoveListener(EquipStem);
            flowerButton?.onClick.RemoveListener(EquipFlower);
            closeGraftButton?.onClick.RemoveListener(CloseGraft);
            resumeButton?.onClick.RemoveListener(TogglePause);
            pauseMenuButton?.onClick.RemoveListener(Menu);
            deadRestartButton?.onClick.RemoveListener(Restart);
            deadMenuButton?.onClick.RemoveListener(Menu);
            resultRetryButton?.onClick.RemoveListener(Restart);
            resultMenuButton?.onClick.RemoveListener(Menu);
            bound = false;
        }
        private void Update()
        {
            if (hud != null && health != null)
            {
                int area = LevelFlow.Current == null ? 0 : Mathf.Clamp(LevelFlow.Current.ActiveEncounter + 1, 1, 3);
                int remaining = LevelFlow.Current == null ? 0 : LevelFlow.Current.ActiveRemaining;
                hud.text = "HP " + Mathf.CeilToInt(health.Current) + "/100    区域 " + area + "/3    敌人 " + remaining + "\nTab 嫁接   Esc 暂停";
            }
            if (interactionText != null) interactionText.gameObject.SetActive(portal != null && portal.CanInteract);
            if (GameBootstrap.Instance == null) return;
            GameState state = GameBootstrap.Instance.State.Current;
            if (state == GameState.Grafting) RefreshGraftText();
        }
        private void ToggleGraft()
        {
            if (GameBootstrap.Instance.State.Current == GameState.Playing) GameBootstrap.Instance.State.SetState(GameState.Grafting);
            else if (GameBootstrap.Instance.State.Current == GameState.Grafting) CloseGraft();
        }
        private void TogglePause()
        {
            GameState state = GameBootstrap.Instance.State.Current;
            if (state == GameState.Grafting) CloseGraft();
            else if (state == GameState.Playing) GameBootstrap.Instance.State.SetState(GameState.Paused);
            else if (state == GameState.Paused) GameBootstrap.Instance.State.SetState(GameState.Playing);
        }
        private void EquipSlot(int index)
        {
            if (GameBootstrap.Instance == null || GameBootstrap.Instance.State.Current != GameState.Grafting) return;
            GraftSlot slot = (GraftSlot)index;
            GraftDefinition item = GameBootstrap.Instance.Session.Inventory.Find(candidate => candidate.Slot == slot);
            if (item != null && applier.TryApply(item)) CloseGraft();
        }
        public void EquipSlotButton(int index) => EquipSlot(index);
        private void EquipRoot() => EquipSlot(0);
        private void EquipStem() => EquipSlot(1);
        private void EquipFlower() => EquipSlot(2);
        public void CloseGraft()
        {
            if (GameBootstrap.Instance == null || GameBootstrap.Instance.State.Current != GameState.Grafting) return;
            GameBootstrap.Instance.State.SetState(GameState.Playing);
            input?.BlockGameplayFor(.20f);
        }
        private void RefreshGraftText()
        {
            if (graftText == null || GameBootstrap.Instance == null) return;
            var session = GameBootstrap.Instance.Session;
            string root = session.Get(GraftSlot.Root)?.DisplayName ?? "空";
            string stem = session.Get(GraftSlot.Stem)?.DisplayName ?? "空";
            string flower = session.Get(GraftSlot.Flower)?.DisplayName ?? "空";
            graftText.text = "嫁接\n根 [1] " + root + "\n茎 [2] " + stem + "\n花 [3] " + flower + "\n\n已获得\n" + string.Join("\n", session.Inventory.ConvertAll(item => item.DisplayName).ToArray());
        }
        private void OnDead()
        {
            if (GameBootstrap.Instance != null) GameBootstrap.Instance.State.SetState(GameState.Dead);
        }

        private IEnumerator RevealDeadPanel()
        {
            yield return new WaitForSecondsRealtime(.8f);
            deadRevealRoutine = null;
            if (GameBootstrap.Instance != null && GameBootstrap.Instance.State.Current == GameState.Dead && deadPanel != null)
                deadPanel.SetActive(true);
        }

        private void OnState(GameState state)
        {
            if (graftPanel != null) graftPanel.SetActive(state == GameState.Grafting);
            if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);
            if (deadRevealRoutine != null) { StopCoroutine(deadRevealRoutine); deadRevealRoutine = null; }
            if (deadPanel != null) deadPanel.SetActive(false);
            if (state == GameState.Dead) deadRevealRoutine = StartCoroutine(RevealDeadPanel());
            if (resultPanel != null) resultPanel.SetActive(state == GameState.Result);
            if (state == GameState.Result && resultText != null)
            {
                var session = GameBootstrap.Instance.Session;
                resultText.text = "关卡完成\n用时 " + session.CompletedSeconds.ToString("F1") + " 秒    击杀 " + session.EnemiesDefeated + "\n根：" + (session.Get(GraftSlot.Root)?.DisplayName ?? "空") + "\n茎：" + (session.Get(GraftSlot.Stem)?.DisplayName ?? "空") + "\n花：" + (session.Get(GraftSlot.Flower)?.DisplayName ?? "空");
            }
            if (state == GameState.Dead && deadText != null) deadText.text = "生命枯萎";
        }
        public void Restart() => GameBootstrap.Instance.StartLevel();
        public void Menu() => GameBootstrap.Instance.ReturnToMenu();
    }
}
