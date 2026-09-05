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
        private bool bossStarted;

        public int ActiveEncounter => activeIndex;
        public int ActiveRemaining => encounters != null && activeIndex >= 0 && activeIndex < encounters.Length ? encounters[activeIndex].Remaining : 0;

        private void Awake() => Current = this;

        private void Start()
        {
            if (GameBootstrap.Instance == null) new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            LevelArtDecorator.Ensure();
            for (int i = 0; i < encounters.Length; i++) encounters[i].Cleared += OnEncounterCleared;
            for (int i = 1; i < encounters.Length; i++) encounters[i].gameObject.SetActive(false);
            GameObject.Find("Gate02")?.SetActive(false);
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
            if (activeIndex >= 1) StartBoss();
            else encounters[activeIndex].UnlockEntry();
        }

        private void StartBoss()
        {
            if (bossStarted) return;
            bossStarted = true;
            GameAudio.PlayBossMusic();
            Vector3 point = portal == null ? Vector3.zero : portal.transform.position + Vector3.left * 3f;
            GameObject shrine = new GameObject("HealingShrine"); shrine.transform.position = point + Vector3.left * 2f;
            shrine.AddComponent<PlaceholderVisual>().Configure(new Color(.35f, 1f, .62f), new Vector2(.7f, 1.1f), 3);
            BoxCollider2D shrineCollider = shrine.AddComponent<BoxCollider2D>(); shrineCollider.isTrigger = true;
            shrine.AddComponent<HealingShrine>();
            GameObject boss = new GameObject("CorruptedAncient"); boss.layer = 12; boss.transform.position = point;
            boss.AddComponent<PlaceholderVisual>().Configure(new Color(.2f, .3f, .12f), new Vector2(2.4f, 3.1f), 3);
            boss.AddComponent<BoxCollider2D>(); Hurtbox2D hurtbox = boss.AddComponent<Hurtbox2D>();
            CorruptedAncientBoss controller = boss.AddComponent<CorruptedAncientBoss>(); hurtbox.Receiver = controller;
            controller.Configure(FindObjectOfType<PlayerMotor2D>()?.transform);
            controller.Defeated += () => StartCoroutine(OpenPortal());
        }

        private IEnumerator OpenPortal()
        {
            foreach (Projectile2D projectile in FindObjectsOfType<Projectile2D>()) Destroy(projectile.gameObject);
            yield return new WaitForSeconds(.8f);
            if (GameBootstrap.Instance.State.Current == GameState.Playing) portal?.BeginOpen();
        }
    }
}
