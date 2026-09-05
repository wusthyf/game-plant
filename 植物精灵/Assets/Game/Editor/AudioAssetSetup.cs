using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace PlantSpirit.GGJ.Editor
{
    public static class AudioAssetSetup
    {
        public const string MixerPath = "Assets/Game/Audio/Resources/PlantSpirit/Audio/PlantSpiritAudioMixer.mixer";
        private const string SfxPath = "Assets/Game/Audio/Resources/PlantSpirit/Audio/SFX";
        private const string MusicPath = "Assets/Game/Audio/Resources/PlantSpirit/Audio/Music";

        [MenuItem("Plant Spirit/Prepare Audio Assets")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            ConfigureSfxImporters();
            ConfigureMusicImporters();
            EnsureMixer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static AudioMixer EnsureMixer()
        {
            AudioMixer existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Assembly editorAssembly = typeof(AudioImporter).Assembly;
            Type controllerType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerController", true);
            object controller = existing;
            if (controller == null)
            {
                MethodInfo create = controllerType.GetMethod("CreateMixerControllerAtPath", BindingFlags.Public | BindingFlags.Static);
                controller = create.Invoke(null, new object[] { MixerPath });
            }
            if (controller == null) throw new InvalidOperationException("Unity could not create the Plant Spirit audio mixer.");

            PropertyInfo masterProperty = controllerType.GetProperty("masterGroup", BindingFlags.Public | BindingFlags.Instance);
            object master = masterProperty.GetValue(controller);
            AudioMixer mixer = controller as AudioMixer;
            object music = FindOrCreateGroup(controllerType, controller, master, mixer, "Music");
            object sfx = FindOrCreateGroup(controllerType, controller, master, mixer, "SFX");
            EnsureDefaultView(editorAssembly, controllerType, controller, master, music, sfx);

            ExposeVolumeIfNeeded(editorAssembly, controllerType, controller, master, "MasterVolume");
            ExposeVolumeIfNeeded(editorAssembly, controllerType, controller, music, "MusicVolume");
            ExposeVolumeIfNeeded(editorAssembly, controllerType, controller, sfx, "SfxVolume");

            UnityEngine.Object asset = controller as UnityEngine.Object;
            if (asset != null)
            {
                asset.name = "PlantSpiritAudioMixer";
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        }

        private static object FindOrCreateGroup(Type controllerType, object controller, object parent, AudioMixer mixer, string name)
        {
            AudioMixerGroup[] matches = mixer == null ? Array.Empty<AudioMixerGroup>() : mixer.FindMatchingGroups(name);
            if (matches.Length > 0) return matches[0];
            object group = controllerType.GetMethod("CreateNewGroup", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(controller, new object[] { name, false });
            controllerType.GetMethod("AddChildToParent", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(controller, new[] { group, parent });
            return group;
        }

        private static void EnsureDefaultView(Assembly editorAssembly, Type controllerType, object controller, params object[] groups)
        {
            Type viewType = editorAssembly.GetType("UnityEditor.Audio.MixerGroupView", true);
            PropertyInfo viewsProperty = controllerType.GetProperty("views", BindingFlags.Public | BindingFlags.Instance);
            Array views = (Array)viewsProperty.GetValue(controller);
            if (views.Length == 0)
            {
                object view = Activator.CreateInstance(viewType);
                viewType.GetField("name", BindingFlags.Public | BindingFlags.Instance).SetValue(view, "All Groups");
                Array initialViews = Array.CreateInstance(viewType, 1);
                initialViews.SetValue(view, 0);
                viewsProperty.SetValue(controller, initialViews);
                controllerType.GetProperty("currentViewIndex", BindingFlags.Public | BindingFlags.Instance).SetValue(controller, 0);
            }

            Type groupType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerGroupController", true);
            PropertyInfo groupId = groupType.GetProperty("groupID", BindingFlags.Public | BindingFlags.Instance);
            Type guidType = groupId.PropertyType;
            Array visible = Array.CreateInstance(guidType, groups.Length);
            for (int i = 0; i < groups.Length; i++) visible.SetValue(groupId.GetValue(groups[i]), i);
            controllerType.GetMethod("SetCurrentViewVisibility", BindingFlags.Public | BindingFlags.Instance)
                .Invoke(controller, new object[] { visible });
            controllerType.GetMethod("SanitizeGroupViews", BindingFlags.Public | BindingFlags.Instance).Invoke(controller, null);
        }

        private static void ExposeVolumeIfNeeded(Assembly editorAssembly, Type controllerType, object controller, object group, string parameterName)
        {
            PropertyInfo exposedProperty = controllerType.GetProperty("exposedParameters", BindingFlags.Public | BindingFlags.Instance);
            Array exposed = (Array)exposedProperty.GetValue(controller);
            Type exposedType = editorAssembly.GetType("UnityEditor.Audio.ExposedAudioParameter", true);
            FieldInfo nameField = exposedType.GetField("name", BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < exposed.Length; i++)
                if (Equals(nameField.GetValue(exposed.GetValue(i)), parameterName)) return;

            Type groupType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerGroupController", true);
            object volumeGuid = groupType.GetMethod("GetGUIDForVolume", BindingFlags.Public | BindingFlags.Instance).Invoke(group, null);
            Type pathType = editorAssembly.GetType("UnityEditor.Audio.AudioGroupParameterPath", true);
            ConstructorInfo pathConstructor = pathType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, new[] { groupType, volumeGuid.GetType() }, null);
            object path = pathConstructor.Invoke(new[] { group, volumeGuid });
            controllerType.GetMethod("AddExposedParameter", BindingFlags.Public | BindingFlags.Instance).Invoke(controller, new[] { path });

            exposed = (Array)exposedProperty.GetValue(controller);
            FieldInfo guidField = exposedType.GetField("guid", BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < exposed.Length; i++)
            {
                object parameter = exposed.GetValue(i);
                if (!Equals(guidField.GetValue(parameter), volumeGuid)) continue;
                nameField.SetValue(parameter, parameterName);
                exposed.SetValue(parameter, i);
                break;
            }
            exposedProperty.SetValue(controller, exposed);
            controllerType.GetMethod("OnChangedExposedParameter", BindingFlags.Public | BindingFlags.Instance).Invoke(controller, null);
        }

        private static void ConfigureSfxImporters()
        {
            string[] clips = AssetDatabase.FindAssets("t:AudioClip", new[] { SfxPath });
            foreach (string guid in clips)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                bool changed = !importer.forceToMono || !settings.preloadAudioData || importer.loadInBackground ||
                    settings.loadType != AudioClipLoadType.DecompressOnLoad ||
                    settings.compressionFormat != AudioCompressionFormat.PCM ||
                    settings.sampleRateSetting != AudioSampleRateSetting.PreserveSampleRate;
                if (!changed) continue;
                importer.forceToMono = true;
                importer.loadInBackground = false;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureMusicImporters()
        {
            string[] clips = AssetDatabase.FindAssets("t:AudioClip", new[] { MusicPath });
            foreach (string guid in clips)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                bool changed = importer.forceToMono || !settings.preloadAudioData || importer.loadInBackground ||
                    settings.loadType != AudioClipLoadType.CompressedInMemory ||
                    settings.compressionFormat != AudioCompressionFormat.Vorbis ||
                    !Mathf.Approximately(settings.quality, .7f);
                if (!changed) continue;
                importer.forceToMono = false;
                importer.loadInBackground = false;
                // Resources.Load during bootstrap must complete before the first frame.
                // CompressedInMemory avoids the startup stall seen with Streaming clips
                // packed into resources.assets while retaining Vorbis compression.
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = .7f;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }
    }
}
