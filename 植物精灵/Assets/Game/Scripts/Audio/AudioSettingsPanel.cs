using UnityEngine;
using UnityEngine.UI;

namespace PlantSpirit.GGJ
{
    public sealed class AudioSettingsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        private bool bound;
        public bool IsOpen => panel != null && panel.activeSelf;

        public void Configure(GameObject targetPanel, Button close, Slider master, Slider music, Slider effects)
        {
            Unbind();
            panel = targetPanel;
            closeButton = close;
            masterSlider = master;
            musicSlider = music;
            sfxSlider = effects;
            Bind();
            Refresh();
        }

        private void OnEnable()
        {
            Bind();
            Refresh();
        }

        private void OnDisable()
        {
            Unbind();
            GameAudioSettings.Save();
        }

        private void Bind()
        {
            if (bound) return;
            closeButton?.onClick.AddListener(Close);
            masterSlider?.onValueChanged.AddListener(SetMaster);
            musicSlider?.onValueChanged.AddListener(SetMusic);
            sfxSlider?.onValueChanged.AddListener(SetSfx);
            GameAudioSettings.Changed += Refresh;
            bound = true;
        }

        private void Unbind()
        {
            if (!bound) return;
            closeButton?.onClick.RemoveListener(Close);
            masterSlider?.onValueChanged.RemoveListener(SetMaster);
            musicSlider?.onValueChanged.RemoveListener(SetMusic);
            sfxSlider?.onValueChanged.RemoveListener(SetSfx);
            GameAudioSettings.Changed -= Refresh;
            bound = false;
        }

        public void Toggle()
        {
            if (panel == null) return;
            panel.SetActive(!panel.activeSelf);
            if (panel.activeSelf) Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            GameAudioSettings.Save();
        }

        private void Refresh()
        {
            masterSlider?.SetValueWithoutNotify(GameAudioSettings.Get(AudioChannel.Master));
            musicSlider?.SetValueWithoutNotify(GameAudioSettings.Get(AudioChannel.Music));
            sfxSlider?.SetValueWithoutNotify(GameAudioSettings.Get(AudioChannel.Sfx));
        }

        private static void SetMaster(float value) => GameAudioSettings.Set(AudioChannel.Master, value);
        private static void SetMusic(float value) => GameAudioSettings.Set(AudioChannel.Music, value);
        private static void SetSfx(float value) => GameAudioSettings.Set(AudioChannel.Sfx, value);
    }
}
