using UnityEngine;
using UnityEngine.UI;

namespace PlantSpirit.GGJ
{
    [RequireComponent(typeof(Button))]
    public sealed class AudioButtonFeedback : MonoBehaviour
    {
        private Button button;

        private void Awake() => button = GetComponent<Button>();

        private void OnEnable()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(PlayClick);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(PlayClick);
        }

        private void PlayClick() => GameAudio.Play(AudioCue.UiClick);
    }
}

