using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PlantSpirit.GGJ.Tests
{
    public sealed class CoreGameplayTests
    {
        private sealed class Receiver : IDamageReceiver
        {
            public int Hits;
            public float Total;
            public bool TryReceive(DamageInfo info) { Hits++; Total += info.Amount; return true; }
        }

        [Test]
        public void KeyboardMovementIncludesWasdAndArrowBindings()
        {
            GameObject inputObject = new GameObject("InputReaderTest");
            InputReader reader = inputObject.AddComponent<InputReader>();
            typeof(InputReader).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(reader, null);
            var field = typeof(InputReader).GetField("move", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            InputAction move = field?.GetValue(reader) as InputAction;
            var paths = new System.Collections.Generic.HashSet<string>();
            if (move != null)
                foreach (InputBinding binding in move.bindings)
                    paths.Add(binding.path);

            Assert.That(move, Is.Not.Null);
            Assert.That(paths, Does.Contain("<Keyboard>/a"));
            Assert.That(paths, Does.Contain("<Keyboard>/d"));
            Assert.That(paths, Does.Contain("<Keyboard>/leftArrow"));
            Assert.That(paths, Does.Contain("<Keyboard>/rightArrow"));
            Object.DestroyImmediate(inputObject);
        }

        [Test]
        public void SameAttackInstanceOnlyDamagesHurtboxOnce()
        {
            GameObject target = new GameObject("Target");
            Hurtbox2D hurtbox = target.AddComponent<Hurtbox2D>();
            Receiver receiver = new Receiver();
            hurtbox.Receiver = receiver;
            var hit = new DamageInfo { AttackInstanceId = 42, Amount = 10f };

            Assert.That(hurtbox.Receive(hit), Is.True);
            Assert.That(hurtbox.Receive(hit), Is.False);
            Assert.That(receiver.Hits, Is.EqualTo(1));
            Assert.That(receiver.Total, Is.EqualTo(10f));
            Object.DestroyImmediate(target);
        }

        [Test]
        public void ReapplyingPoisonRefreshesDurationWithoutStackingDamage()
        {
            GameObject target = new GameObject("PoisonTarget");
            Hurtbox2D hurtbox = target.AddComponent<Hurtbox2D>();
            Receiver receiver = new Receiver();
            hurtbox.Receiver = receiver;
            StatusController status = target.AddComponent<StatusController>();

            status.ApplyPoisonAt(6f, 3f, .3f, 0f);
            status.ApplyPoisonAt(6f, 3f, .3f, .5f);
            status.TickAt(1f);
            status.TickAt(1.5f);
            status.TickAt(2f);

            Assert.That(receiver.Hits, Is.EqualTo(2));
            Assert.That(receiver.Total, Is.EqualTo(12f));
            Assert.That(status.SlowPercent, Is.EqualTo(.3f).Within(.001f));
            Object.DestroyImmediate(target);
        }

        [Test]
        public void ThreeSecondPoisonDealsThreeTicksAndThenExpires()
        {
            GameObject target = new GameObject("PoisonDurationTarget");
            Hurtbox2D hurtbox = target.AddComponent<Hurtbox2D>();
            Receiver receiver = new Receiver();
            hurtbox.Receiver = receiver;
            StatusController status = target.AddComponent<StatusController>();

            status.ApplyPoisonAt(6f, 3f, .3f, 0f);
            status.TickAt(1f);
            status.TickAt(2f);
            status.TickAt(3f);

            Assert.That(receiver.Hits, Is.EqualTo(3));
            Assert.That(receiver.Total, Is.EqualTo(18f));
            Assert.That(status.SlowPercent, Is.Zero);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void IronRootReducesTwelveDamageToNine()
        {
            GameObject player = new GameObject("Player");
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<BoxCollider2D>();
            PlayerMotor2D motor = player.AddComponent<PlayerMotor2D>();
            PlayerConfig config = ScriptableObject.CreateInstance<PlayerConfig>();
            motor.Configure(config, 0, null);
            PlayerHealth health = player.AddComponent<PlayerHealth>();
            GameSession session = new GameSession();
            health.Configure(config, motor, session);
            GraftDefinition root = ScriptableObject.CreateInstance<GraftDefinition>();
            root.Slot = GraftSlot.Root;
            root.DamageReduction = .25f;
            session.Equip(root);

            Assert.That(health.TryReceive(new DamageInfo { Amount = 12f }), Is.True);
            Assert.That(health.Current, Is.EqualTo(91f).Within(.001f));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(player);
        }

        [Test]
        public void RequiredContentAssetsExistAndHaveUniqueIds()
        {
            string[] graftPaths = AssetDatabase.FindAssets("t:GraftDefinition", new[] { "Assets/Game/Data" });
            Assert.That(graftPaths.Length, Is.EqualTo(3));
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (string guid in graftPaths)
            {
                GraftDefinition graft = AssetDatabase.LoadAssetAtPath<GraftDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(graft.Id, Is.Not.Empty);
                Assert.That(ids.Add(graft.Id), Is.True);
            }
            Assert.That(AssetDatabase.FindAssets("t:AttackDefinition", new[] { "Assets/Game/Data" }).Length, Is.EqualTo(4));
        }

        [Test]
        public void AudioVolumeConversionUsesLogarithmicDecibels()
        {
            Assert.That(GameAudioSettings.ToDecibels(1f), Is.EqualTo(0f).Within(.001f));
            Assert.That(GameAudioSettings.ToDecibels(.5f), Is.EqualTo(-6.0206f).Within(.001f));
            Assert.That(GameAudioSettings.ToDecibels(0f), Is.EqualTo(-80f));
        }

        [Test]
        public void AudioMixerAndLicensedSfxAreConfigured()
        {
            const string audioRoot = "Assets/Game/Audio/Resources/PlantSpirit/Audio/SFX";
            string[] clips = AssetDatabase.FindAssets("t:AudioClip", new[] { audioRoot });
            Assert.That(clips.Length, Is.EqualTo(15));
            foreach (string guid in clips)
            {
                AudioImporter importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as AudioImporter;
                Assert.That(importer, Is.Not.Null);
                Assert.That(importer.forceToMono, Is.True);
                Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.True);
                Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
                Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.PCM));
            }

            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Editor.AudioAssetSetup.MixerPath);
            Assert.That(mixer, Is.Not.Null);
            AudioMixerGroup[] groups = mixer.FindMatchingGroups("Master");
            Assert.That(groups.Length, Is.EqualTo(3));
            Assert.That(System.Array.Exists(groups, group => group.name == "Master"), Is.True);
            Assert.That(mixer.FindMatchingGroups("Music").Length, Is.EqualTo(1));
            Assert.That(mixer.FindMatchingGroups("SFX").Length, Is.EqualTo(1));
            SerializedProperty exposed = new SerializedObject(mixer).FindProperty("m_ExposedParameters");
            Assert.That(exposed.arraySize, Is.EqualTo(3));
            var names = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < exposed.arraySize; i++)
                names.Add(exposed.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
            CollectionAssert.AreEquivalent(new[] { "MasterVolume", "MusicVolume", "SfxVolume" }, names);
            SerializedProperty views = new SerializedObject(mixer).FindProperty("m_AudioMixerGroupViews");
            Assert.That(views.arraySize, Is.EqualTo(1));
            Assert.That(views.GetArrayElementAtIndex(0).FindPropertyRelative("guids").arraySize, Is.EqualTo(3));
        }

        [Test]
        public void SuppliedArtIsImportedWithExpectedSequencesAndSettings()
        {
            const string artRoot = "Assets/Game/Art/Resources/PlantSpirit";
            string[] textures = AssetDatabase.FindAssets("t:Texture2D", new[] { artRoot });
            Assert.That(textures.Length, Is.EqualTo(168));
            Assert.That(Resources.LoadAll<Sprite>("PlantSpirit/Player/AttackA").Length, Is.EqualTo(6));
            Assert.That(Resources.LoadAll<Sprite>("PlantSpirit/Player/AttackB").Length, Is.EqualTo(7));
            Assert.That(Resources.LoadAll<Sprite>("PlantSpirit/Enemies/Vine/Idle").Length, Is.EqualTo(4));
            Assert.That(Resources.LoadAll<Sprite>("PlantSpirit/Enemies/Mushroom/Walk").Length, Is.EqualTo(7));
            Assert.That(Resources.LoadAll<Sprite>("PlantSpirit/Vfx/Spore").Length, Is.EqualTo(4));
            Assert.That(Resources.LoadAll<Sprite>("PlantSpirit/Vfx/Burst").Length, Is.EqualTo(6));

            string samplePath = AssetDatabase.GUIDToAssetPath(textures[0]);
            TextureImporter importer = AssetImporter.GetAtPath(samplePath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(256f));
        }

        [Test]
        public void RequiredPhysicsLayersAreConfigured()
        {
            Assert.That(LayerMask.NameToLayer("Ground"), Is.EqualTo(8));
            Assert.That(LayerMask.NameToLayer("Player"), Is.EqualTo(9));
            Assert.That(LayerMask.NameToLayer("PlayerProjectile"), Is.EqualTo(10));
            Assert.That(LayerMask.NameToLayer("EnemyProjectile"), Is.EqualTo(11));
            Assert.That(LayerMask.NameToLayer("Enemy"), Is.EqualTo(12));
            Assert.That(LayerMask.NameToLayer("Interactable"), Is.EqualTo(14));
        }

        [Test]
        public void FormalLevelHasRequiredStructureAndReachablePlatforms()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/Level01.unity", OpenSceneMode.Single);
            PlayerMotor2D player = Object.FindObjectOfType<PlayerMotor2D>();
            EncounterZone[] encounters = Object.FindObjectsOfType<EncounterZone>(true);
            ExitPortal portal = Object.FindObjectOfType<ExitPortal>(true);
            GameUiController ui = Object.FindObjectOfType<GameUiController>(true);
            Button[] buttons = Object.FindObjectsOfType<Button>(true);
            PlayerConfig config = AssetDatabase.LoadAssetAtPath<PlayerConfig>("Assets/Game/Data/PlayerConfig.asset");
            float jumpHeight = config.JumpVelocity * config.JumpVelocity / (2f * Mathf.Abs(Physics2D.gravity.y) * 3.2f);
            float groundTop = GameObject.Find("Ground").GetComponent<Collider2D>().bounds.max.y;
            string[] platformNames = { "TutorialPlatform", "CombatPlatform", "CombatPlatform02", "CombatPlatform03" };

            Assert.That(player, Is.Not.Null);
            Assert.That(config.GroundDeceleration, Is.EqualTo(55f).Within(.001f));
            Assert.That(config.MaxFallSpeed, Is.EqualTo(18f).Within(.001f));
            Assert.That(encounters.Length, Is.EqualTo(3));
            Assert.That(portal, Is.Not.Null);
            Assert.That(ui, Is.Not.Null);
            Assert.That(buttons.Length, Is.GreaterThanOrEqualTo(10));
            foreach (string platformName in platformNames)
            {
                float rise = GameObject.Find(platformName).GetComponent<Collider2D>().bounds.max.y - groundTop;
                Assert.That(jumpHeight - rise, Is.GreaterThan(.35f), platformName + " must leave at least 0.35 units of jump margin.");
            }

            SerializedObject uiData = new SerializedObject(ui);
            Assert.That(uiData.FindProperty("rootButton").objectReferenceValue, Is.Not.Null);
            Assert.That(uiData.FindProperty("resultMenuButton").objectReferenceValue, Is.Not.Null);
            AssertSceneHasNoMissingScripts(scene);
        }

        [Test]
        public void MainMenuPresenterHasSerializedControls()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/MainMenu.unity", OpenSceneMode.Single);
            MainMenuPresenter presenter = Object.FindObjectOfType<MainMenuPresenter>(true);
            Assert.That(presenter, Is.Not.Null);
            SerializedObject data = new SerializedObject(presenter);
            Assert.That(data.FindProperty("startButton").objectReferenceValue, Is.Not.Null);
            Assert.That(data.FindProperty("controlsButton").objectReferenceValue, Is.Not.Null);
            Assert.That(data.FindProperty("audioButton").objectReferenceValue, Is.Not.Null);
            Assert.That(data.FindProperty("quitButton").objectReferenceValue, Is.Not.Null);
            Assert.That(data.FindProperty("controlsPanel").objectReferenceValue, Is.Not.Null);
            Assert.That(data.FindProperty("audioSettingsPanel").objectReferenceValue, Is.Not.Null);
            AudioSettingsPanel audioSettings = Object.FindObjectOfType<AudioSettingsPanel>(true);
            Assert.That(audioSettings, Is.Not.Null);
            SerializedObject settingsData = new SerializedObject(audioSettings);
            Assert.That(settingsData.FindProperty("masterSlider").objectReferenceValue, Is.Not.Null);
            Assert.That(settingsData.FindProperty("musicSlider").objectReferenceValue, Is.Not.Null);
            Assert.That(settingsData.FindProperty("sfxSlider").objectReferenceValue, Is.Not.Null);
            AssertSceneHasNoMissingScripts(scene);
        }

        [Test]
        public void TerminalStatesRejectGameplayTransitions()
        {
            var state = new GameStateController();
            Assert.That(state.SetState(GameState.Playing), Is.True);
            Assert.That(state.SetState(GameState.Dead), Is.True);
            Assert.That(state.SetState(GameState.Grafting), Is.False);
            Assert.That(state.Current, Is.EqualTo(GameState.Dead));
        }

        private static void AssertSceneHasNoMissingScripts(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject), Is.Zero,
                        item.gameObject.name + " contains a missing MonoBehaviour reference.");
        }
    }
}
