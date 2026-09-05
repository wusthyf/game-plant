using System.Collections;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class ExitPortal : MonoBehaviour
    {
        private bool open;
        private bool entering;
        private PlayerMotor2D playerInside;
        private InputReader input;
        private string destinationScene;

        public bool IsOpen => open;
        public bool IsEntering => entering;
        public bool CanInteract => open && playerInside != null && !entering;

        private void Start()
        {
            input = FindObjectOfType<InputReader>();
            if (input != null) input.Interact += TryEnter;
        }

        private void OnDestroy()
        {
            if (input != null) input.Interact -= TryEnter;
        }

        public void BeginOpen()
        {
            open = true;
            WorldArtPresentation2D.AttachPortal(gameObject);
            gameObject.SetActive(true);
            GameAudio.Play(AudioCue.PortalGrow);
        }

        public void ConfigureDestination(string sceneName)
        {
            destinationScene = sceneName;
        }

        public void TryEnter()
        {
            if (!open || playerInside == null || entering || GameBootstrap.Instance == null || GameBootstrap.Instance.State.Current != GameState.Playing) return;
            entering = true;
            input?.SetGameplayLocked(true);
            playerInside.LockControl();
            playerInside.GetComponent<PlayerCombat>()?.LockActions();
            StartCoroutine(Enter());
        }

        private IEnumerator Enter()
        {
            yield return new WaitForSeconds(.6f);
            if (GameBootstrap.Instance == null) yield break;
            if (string.IsNullOrEmpty(destinationScene)) GameBootstrap.Instance.FinishRun();
            else GameBootstrap.Instance.LoadLevel(destinationScene);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerMotor2D player = other.GetComponent<PlayerMotor2D>();
            if (player != null) playerInside = player;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerMotor2D player = other.GetComponent<PlayerMotor2D>();
            if (player != null && player == playerInside) playerInside = null;
        }
    }
}
