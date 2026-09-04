using System.Collections;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class LevelFlow : MonoBehaviour
    {
        public static LevelFlow Current { get; private set; }
        [SerializeField] private EncounterZone[] encounters;
        [SerializeField] private ExitPortal portal;
        private int activeIndex;

        public int ActiveEncounter => activeIndex;
        public int ActiveRemaining => encounters != null && activeIndex >= 0 && activeIndex < encounters.Length ? encounters[activeIndex].Remaining : 0;

        private void Awake() => Current = this;

        private void Start()
        {
            if (GameBootstrap.Instance == null) new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            LevelArtDecorator.Ensure();
            for (int i = 0; i < encounters.Length; i++) encounters[i].Cleared += OnEncounterCleared;
            GameBootstrap.Instance.BeginRun();
        }

        private void OnDestroy()
        {
            if (encounters != null)
                for (int i = 0; i < encounters.Length; i++)
                    if (encounters[i] != null) encounters[i].Cleared -= OnEncounterCleared;
            if (Current == this) Current = null;
        }

        public void RequestEncounter(EncounterZone encounter)
        {
            if (GameBootstrap.Instance.State.Current != GameState.Playing || encounter.Order != activeIndex) return;
            encounter.Begin();
        }

        private void OnEncounterCleared(EncounterZone encounter)
        {
            if (encounter.Order != activeIndex) return;
            activeIndex++;
            if (activeIndex >= encounters.Length) StartCoroutine(OpenPortal());
            else encounters[activeIndex].UnlockEntry();
        }

        private IEnumerator OpenPortal()
        {
            foreach (Projectile2D projectile in FindObjectsOfType<Projectile2D>()) Destroy(projectile.gameObject);
            yield return new WaitForSeconds(.8f);
            if (GameBootstrap.Instance.State.Current == GameState.Playing) portal?.BeginOpen();
        }
    }
}
