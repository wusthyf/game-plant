using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlantSpirit.GGJ
{
    public sealed class LevelFlow : MonoBehaviour
    {
        public static LevelFlow Current { get; private set; }
        [SerializeField] private EncounterZone[] encounters;
        [SerializeField] private ExitPortal portal;
        private int activeIndex;
        private bool bossStarted;
        private bool isBossArena;

        public int ActiveEncounter => activeIndex;
        public int ActiveRemaining => encounters != null && activeIndex >= 0 && activeIndex < encounters.Length ? encounters[activeIndex].Remaining : 0;

        private void Awake() => Current = this;

        private void Start()
        {
            if (GameBootstrap.Instance == null) new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            isBossArena = SceneManager.GetActiveScene().name == "BossArena";
            LevelArtDecorator.Ensure();
            if (isBossArena)
            {
                for (int i = 0; i < encounters.Length; i++) encounters[i].gameObject.SetActive(false);
                portal?.gameObject.SetActive(false);
                GameBootstrap.Instance.ContinueRun();
                StartBoss();
                return;
            }
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
            if (activeIndex >= 1) OpenBossPortal();
            else encounters[activeIndex].UnlockEntry();
        }

        private void OpenBossPortal()
        {
            portal?.ConfigureDestination("BossArena");
            portal?.BeginOpen();
        }

        private void StartBoss()
        {
            if (bossStarted) return;
            bossStarted = true;
            GameAudio.PlayBossMusic();
            PlayerMotor2D player = FindObjectOfType<PlayerMotor2D>();
            if (isBossArena && player != null) player.transform.position = new Vector3(-5f, -3.1f, 0f);
            Vector3 point = isBossArena ? new Vector3(4f, -2.9f, 0f) : portal == null ? Vector3.zero : portal.transform.position + Vector3.left * 3f;
            BossFogTrigger fog = isBossArena ? CreateBossApproach(point) : null;
            GameObject boss = new GameObject("CorruptedAncient"); boss.layer = 12; boss.transform.position = point;
            boss.AddComponent<PlaceholderVisual>().Configure(new Color(.2f, .3f, .12f), new Vector2(2.4f, 3.1f), 3);
            boss.AddComponent<BoxCollider2D>(); Hurtbox2D hurtbox = boss.AddComponent<Hurtbox2D>();
            CorruptedAncientBoss controller = boss.AddComponent<CorruptedAncientBoss>(); hurtbox.Receiver = controller;
            controller.Configure(player?.transform, !isBossArena);
            if (fog != null) fog.PlayerEntered += controller.BeginBattle;
            controller.Defeated += () => StartCoroutine(OpenPortal());
        }

        private BossFogTrigger CreateBossApproach(Vector3 bossPoint)
        {
            Vector3 shrinePoint = bossPoint + Vector3.left * 6f;
            GameObject shrine = new GameObject("HealingShrine"); shrine.transform.position = shrinePoint;
            shrine.AddComponent<PlaceholderVisual>().Configure(new Color(.35f, 1f, .62f), new Vector2(.7f, 1.1f), 3);
            BoxCollider2D shrineCollider = shrine.AddComponent<BoxCollider2D>(); shrineCollider.isTrigger = true;
            shrine.AddComponent<HealingShrine>();

            GameObject fog = new GameObject("BossFogWall"); fog.layer = 14; fog.transform.position = bossPoint + Vector3.left * 3.2f;
            BoxCollider2D fogCollider = fog.AddComponent<BoxCollider2D>(); fogCollider.isTrigger = true; fogCollider.size = new Vector2(1.45f, 4.8f);
            fog.AddComponent<BossFogVisual>().Configure();
            BossFogTrigger trigger = fog.AddComponent<BossFogTrigger>();
            GameObject promptObject = new GameObject("BossApproachPrompt", typeof(RectTransform));
            BossApproachPrompt prompt = promptObject.AddComponent<BossApproachPrompt>();
            trigger.Configure(prompt);

            GameObject nest = new GameObject("CorruptedAncientNest"); nest.transform.position = bossPoint + Vector3.up * .15f;
            nest.AddComponent<PlaceholderVisual>().Configure(new Color(.16f, .09f, .2f, .65f), new Vector2(5.2f, 3.8f), 1);
            return trigger;
        }

        private IEnumerator OpenPortal()
        {
            foreach (Projectile2D projectile in FindObjectsOfType<Projectile2D>()) Destroy(projectile.gameObject);
            yield return new WaitForSeconds(.8f);
            if (GameBootstrap.Instance.State.Current == GameState.Playing) portal?.BeginOpen();
        }
    }
}
