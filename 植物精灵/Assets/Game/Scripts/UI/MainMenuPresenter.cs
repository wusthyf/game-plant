using UnityEngine;
using UnityEngine.UI;

namespace PlantSpirit.GGJ
{
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject controlsPanel;

        public void Configure(Button start, Button controls, Button quit, GameObject panel)
        {
            startButton = start;
            controlsButton = controls;
            quitButton = quit;
            controlsPanel = panel;
        }

        private void OnEnable()
        {
            startButton?.onClick.AddListener(StartGame);
            controlsButton?.onClick.AddListener(ToggleControls);
            quitButton?.onClick.AddListener(QuitGame);
        }

        private void Start() => MenuArtDecorator.Ensure();

        private void OnDisable()
        {
            startButton?.onClick.RemoveListener(StartGame);
            controlsButton?.onClick.RemoveListener(ToggleControls);
            quitButton?.onClick.RemoveListener(QuitGame);
        }

        public void StartGame() => GameBootstrap.Instance?.StartLevel();
        public void ToggleControls() { if (controlsPanel != null) controlsPanel.SetActive(!controlsPanel.activeSelf); }
        public void QuitGame() => Application.Quit();
    }
}
