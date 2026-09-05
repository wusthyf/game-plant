using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace PlantSpirit.GGJ
{
    [DefaultExecutionOrder(1000)]
    public sealed class RuntimeSmokeDriver : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;
            MainMenuPresenter menu = FindObjectOfType<MainMenuPresenter>();
            AudioSettingsPanel audioSettings = FindObjectOfType<AudioSettingsPanel>();
            if (!Require(menu != null && audioSettings != null && GameObject.Find("MenuArt") != null && GameAudio.Ready && GameAudio.MusicReady && GameAudio.Instance.HasAudioListener && GameAudio.Instance.IsMusicPlaying && GameAudio.Instance.CurrentMusicClipName == "menu_music" && GameAudio.Instance.MixerSettingsApplied && GameAudio.IsCueAvailable(AudioCue.UiClick), "Main menu, supplied art, listener, music, or audio system missing")) yield break;
            menu.ToggleAudio();
            yield return null;
            if (!Require(audioSettings.IsOpen, "Audio settings panel did not open")) yield break;
            menu.ToggleAudio();
            yield return null;
            if (!Require(!audioSettings.IsOpen, "Audio settings panel did not close")) yield break;
            menu.StartGame();

            float deadline = Time.realtimeSinceStartup + 8f;
            while ((SceneManager.GetActiveScene().name != "Level01" || GameBootstrap.Instance.State.Current != GameState.Playing) && Time.realtimeSinceStartup < deadline) yield return null;
            if (!Require(SceneManager.GetActiveScene().name == "Level01", "Level01 did not load")) yield break;
            if (!Require(GameAudio.Instance.IsMusicPlaying && GameAudio.Instance.CurrentMusicClipName == "level_music", "Level music did not start")) yield break;

            LevelFlow flow = FindObjectOfType<LevelFlow>();
            PlayerMotor2D player = FindObjectOfType<PlayerMotor2D>();
            GraftInventory inventory = player == null ? null : player.GetComponent<GraftInventory>();
            GraftApplier applier = player == null ? null : player.GetComponent<GraftApplier>();
            PlayerCombat combat = player == null ? null : player.GetComponent<PlayerCombat>();
            PlayerHealth health = player == null ? null : player.GetComponent<PlayerHealth>();
            Hurtbox2D playerHurtbox = player == null ? null : player.GetComponent<Hurtbox2D>();
            PlayerArtController playerArt = player == null ? null : player.GetComponent<PlayerArtController>();
            InputReader input = FindObjectOfType<InputReader>();
            CameraFollow2D cameraFollow = FindObjectOfType<CameraFollow2D>();
            LevelArtDecorator levelArt = FindObjectOfType<LevelArtDecorator>();
            Camera gameplayCamera = Camera.main;
            EncounterZone[] zones = FindObjectsOfType<EncounterZone>();
            Array.Sort(zones, (a, b) => a.Order.CompareTo(b.Order));
            if (!Require(flow != null && player != null && inventory != null && applier != null && combat != null && health != null && playerHurtbox != null && playerArt != null && playerArt.HasArt && input != null && cameraFollow != null && gameplayCamera != null && levelArt != null && zones.Length == 3, "Level structure or supplied art incomplete")) yield break;
            if (!Require(ReferenceEquals(playerHurtbox.Receiver, health), "Player hurtbox is not connected to player health")) yield break;
            if (!Require(
                ArtResources2D.LoadSequence("Player/AttackA").Length == 6 &&
                ArtResources2D.LoadSequence("Player/AttackB").Length == 7 &&
                ArtResources2D.LoadSequence("Enemies/Vine/Death").Length == 6 &&
                ArtResources2D.LoadSequence("Enemies/Mushroom/Walk").Length == 7 &&
                ArtResources2D.LoadSequence("Vfx/Spore").Length == 4 &&
                ArtResources2D.LoadSequence("Vfx/Burst").Length == 6,
                "Supplied animation sequences are incomplete")) yield break;
            Vector3 playerViewport = gameplayCamera.WorldToViewportPoint(player.transform.position);
            if (!Require(cameraFollow.Target == player.transform && playerViewport.z > 0f && playerViewport.x >= 0f && playerViewport.x <= 1f && playerViewport.y >= 0f && playerViewport.y <= 1f, "Player is outside the gameplay camera")) yield break;

            Keyboard keyboard = Keyboard.current;
            bool removeKeyboard = keyboard == null;
            if (removeKeyboard) keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow));
            yield return null;
            yield return new WaitForFixedUpdate();
            bool keyboardMovedPlayer = player.GetComponent<Rigidbody2D>().velocity.x > .1f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
            yield return null;
            bool graftOpenedFromKeyboard = GameBootstrap.Instance.State.Current == GameState.Grafting;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
            yield return null;
            bool graftClosedFromKeyboard = GameBootstrap.Instance.State.Current == GameState.Playing;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            if (removeKeyboard) InputSystem.RemoveDevice(keyboard);
            if (!Require(player.InputBound && combat.InputBound && keyboardMovedPlayer && graftOpenedFromKeyboard && graftClosedFromKeyboard, "Arrow-key movement or graft toggle input is not connected")) yield break;
            input.enabled = false;
            player.SetMove(0f);

            deadline = Time.realtimeSinceStartup + 2f;
            while (!player.Grounded && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            if (!Require(player.Grounded, "Player did not settle on the ground")) yield break;
            player.SetMove(1f);
            deadline = Time.realtimeSinceStartup + 2f;
            while (player.transform.position.x < -11.35f && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            player.BufferJump();
            yield return null;
            if (!Require(player.GetComponent<Rigidbody2D>().velocity.y > 0f, "Jump buffer did not produce upward velocity")) yield break;
            deadline = Time.realtimeSinceStartup + 2f;
            while (player.transform.position.x < -8.15f && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            player.SetMove(0f);
            deadline = Time.realtimeSinceStartup + 2.5f;
            while (!player.Grounded && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            if (!Require(player.Grounded && player.transform.position.y > -2.35f, "Player could not land on the tutorial platform at " + player.transform.position)) yield break;

            Teleport(player, new Vector3(2.55f, -3.15f, 0f));
            yield return new WaitForFixedUpdate();
            player.SetMove(1f);
            yield return new WaitForFixedUpdate();
            player.SetMove(0f);
            if (!Require(player.BeginDash(), "Dash could not start")) yield break;
            yield return new WaitForSeconds(.35f);
            if (!Require(player.transform.position.x < 3.2f, "Dash passed through the closed encounter gate")) yield break;
            Teleport(player, new Vector3(-13f, -3.15f, 0f));

            for (int i = 0; i < zones.Length; i++)
            {
                Teleport(player, new Vector3(zones[i].transform.position.x - 3f, -3.15f, 0f));
                yield return new WaitForFixedUpdate();
                yield return null;
                EnemyController[] enemies = FindObjectsOfType<EnemyController>();
                if (!Require(zones[i].Started && enemies.Length > 0, "Encounter " + (i + 1) + " did not spawn enemies")) yield break;
                foreach (EnemyController enemy in enemies)
                {
                    if (enemy.Dead || enemy.Kind == EnemyKind.Beetle) continue;
                    EnemyArtController enemyArt = enemy.GetComponent<EnemyArtController>();
                    if (!Require(enemyArt != null && enemyArt.HasArt, enemy.Kind + " supplied art missing")) yield break;
                }
                foreach (EnemyController enemy in enemies) if (!enemy.Dead) enemy.TryReceive(new DamageInfo { Amount = 9999f, Source = gameObject });
                if (!Require(zones[i].ClearedState, "Encounter " + (i + 1) + " did not clear")) yield break;

                GraftPickup pickup = FindObjectOfType<GraftPickup>();
                if (!Require(pickup != null && pickup.transform.Find("PickupArt") != null, "Encounter " + (i + 1) + " reward or supplied art missing")) yield break;
                Teleport(player, pickup.transform.position);
                yield return new WaitForFixedUpdate();
                yield return null;
                if (!Require(GameBootstrap.Instance.Session.Inventory.Count == i + 1, "Encounter " + (i + 1) + " reward was not collected")) yield break;
            }

            foreach (GraftDefinition graft in GameBootstrap.Instance.Session.Inventory)
                if (!Require(applier.TryApply(graft), "Could not equip " + graft.Id)) yield break;
            if (!Require(GameBootstrap.Instance.Session.Loadout.Count == 3, "All graft slots were not equipped")) yield break;

            float healthBefore = health.Current;
            health.TryReceive(new DamageInfo { Amount = 12f, Source = gameObject });
            if (!Require(Mathf.Approximately(healthBefore - health.Current, 9f), "Iron Root did not reduce 12 damage to 9")) yield break;

            EnemyController vineTarget = CreateTarget(player.transform.position + Vector3.right * 1.7f);
            if (!Require(combat.RequestAttack(), "Vine attack could not start")) yield break;
            yield return new WaitForSeconds(.45f);
            if (!Require(Mathf.Approximately(vineTarget.CurrentHealth, 92f), "Vine attack did not deal 8 damage")) yield break;
            Destroy(vineTarget.gameObject);

            EnemyController poisonTarget = CreateTarget(player.transform.position + Vector3.right * 2f);
            StatusController poisonStatus = poisonTarget.GetComponent<StatusController>();
            if (!Require(combat.RequestSkill(), "Poison skill could not start")) yield break;
            yield return new WaitForSeconds(1.4f);
            if (!Require(poisonTarget.CurrentHealth < 100f && Mathf.Approximately(poisonStatus.SlowPercent, .3f), "Poison skill did not apply DOT and 30% slow")) yield break;
            yield return new WaitForSeconds(3.1f);
            if (!Require(Mathf.Approximately(poisonTarget.CurrentHealth, 82f) && Mathf.Approximately(poisonStatus.SlowPercent, 0f),
                "Three-second poison expected health=82 slow=0 but was health=" + poisonTarget.CurrentHealth + " slow=" + poisonStatus.SlowPercent)) yield break;
            Destroy(poisonTarget.gameObject);

            yield return new WaitForSeconds(1f);
            ExitPortal portal = FindObjectOfType<ExitPortal>(true);
            if (!Require(portal != null && portal.IsOpen && portal.transform.Find("PortalArch") != null, "Portal or supplied portal art did not open")) yield break;
            Teleport(player, portal.transform.position);
            yield return new WaitForFixedUpdate();
            yield return null;
            if (!Require(portal.CanInteract, "Portal interaction range did not register")) yield break;
            portal.TryEnter();
            if (!Require(portal.IsEntering && !player.BeginDash() && !combat.RequestAttack(), "Portal entry did not lock player actions")) yield break;
            yield return new WaitForSecondsRealtime(.8f);
            if (!Require(GameBootstrap.Instance.State.Current == GameState.Result, "Result state was not reached")) yield break;

            GameBootstrap.Instance.StartLevel();
            deadline = Time.realtimeSinceStartup + 8f;
            while ((SceneManager.GetActiveScene().name != "Level01" || GameBootstrap.Instance.State.Current != GameState.Playing) && Time.realtimeSinceStartup < deadline) yield return null;
            GameSession restarted = GameBootstrap.Instance.Session;
            if (!Require(SceneManager.GetActiveScene().name == "Level01" && restarted.Inventory.Count == 0 && restarted.Loadout.Count == 0 && restarted.EnemiesDefeated == 0 && !restarted.Completed && Mathf.Approximately(Time.timeScale, 1f), "Restart did not reset the run")) yield break;

            PlayerHealth restartedHealth = FindObjectOfType<PlayerHealth>();
            if (!Require(restartedHealth != null && restartedHealth.TryReceive(new DamageInfo { Amount = 9999f, Source = gameObject }), "Restarted player could not receive lethal damage")) yield break;
            yield return null;
            if (!Require(GameBootstrap.Instance.State.Current == GameState.Dead, "Death did not enter its terminal state immediately")) yield break;
            yield return new WaitForSecondsRealtime(.9f);
            if (!Require(Mathf.Approximately(Time.timeScale, 0f), "Death presentation did not freeze after its delay")) yield break;

            GameBootstrap.Instance.ReturnToMenu();
            deadline = Time.realtimeSinceStartup + 8f;
            while ((SceneManager.GetActiveScene().name != "MainMenu" || GameBootstrap.Instance.State.Current != GameState.MainMenu) && Time.realtimeSinceStartup < deadline) yield return null;
            if (!Require(SceneManager.GetActiveScene().name == "MainMenu" && FindObjectOfType<MainMenuPresenter>() != null, "Return to menu failed")) yield break;

            Debug.Log("[PlantSpiritSmoke] PASS SuppliedArt-AudioSettings-Audio-PlayerVisible-ArrowInput-GraftToggle-PlayerHurtbox-PlatformJump-DashGate-3EncounterTriggers-3Grafts-GraftEffects-PortalLock-Result-Restart-DeathDelay-Menu");
            Application.Quit(0);
        }

        private static void Teleport(PlayerMotor2D player, Vector3 destination)
        {
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            player.transform.position = destination;
            Physics2D.SyncTransforms();
        }

        private static EnemyController CreateTarget(Vector3 position)
        {
            GameObject target = new GameObject("SmokeTarget");
            target.layer = 12;
            target.transform.position = position;
            target.AddComponent<BoxCollider2D>().size = new Vector2(.6f, .8f);
            target.AddComponent<Hurtbox2D>();
            target.AddComponent<StatusController>();
            EnemyController enemy = target.AddComponent<EnemyController>();
            enemy.Configure(EnemyKind.Vine, null, 100f, 0f, 0f);
            Physics2D.SyncTransforms();
            return enemy;
        }

        private static bool Require(bool condition, string message)
        {
            if (condition) return true;
            Debug.LogError("[PlantSpiritSmoke] FAIL " + message);
            Application.Quit(3);
            return false;
        }
    }
}
