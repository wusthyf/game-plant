using UnityEngine;
using UnityEngine.UI;

namespace PlantSpirit.GGJ
{
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button audioButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private AudioSettingsPanel audioSettingsPanel;

        public void Configure(Button start, Button controls, Button audio, Button quit, GameObject panel, AudioSettingsPanel settingsPanel)
        {
            startButton = start;
            controlsButton = controls;
            audioButton = audio;
            quitButton = quit;
            controlsPanel = panel;
            audioSettingsPanel = settingsPanel;
        }

        private void OnEnable()
        {
            startButton?.onClick.AddListener(StartGame);
            controlsButton?.onClick.AddListener(ToggleControls);
            audioButton?.onClick.AddListener(ToggleAudio);
            quitButton?.onClick.AddListener(QuitGame);
        }

        private void Start() => MenuArtDecorator.Ensure();

        private void OnDisable()
        {
            startButton?.onClick.RemoveListener(StartGame);
            controlsButton?.onClick.RemoveListener(ToggleControls);
            audioButton?.onClick.RemoveListener(ToggleAudio);
            quitButton?.onClick.RemoveListener(QuitGame);
        }

        public void StartGame() => GameBootstrap.Instance?.StartLevel();
        public void ToggleControls()
        {
            audioSettingsPanel?.Close();
            if (controlsPanel != null) controlsPanel.SetActive(!controlsPanel.activeSelf);
        }
        public void ToggleAudio()
        {
            if (controlsPanel != null) controlsPanel.SetActive(false);
            audioSettingsPanel?.Toggle();
        }
        public void QuitGame() => Application.Quit();
    }
}
