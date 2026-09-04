using System.IO;
using PlantSpirit.GGJ;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlantSpirit.GGJ.Editor
{
    public static class GGJSceneGenerator
    {
        private const string SceneDirectory = "Assets/Game/Scenes";

        [MenuItem("Plant Spirit/Create GGJ48H Scenes")]
        public static void CreateScenes()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Game")) AssetDatabase.CreateFolder("Assets", "Game");
            if (!AssetDatabase.IsValidFolder(SceneDirectory)) AssetDatabase.CreateFolder("Assets/Game", "Scenes");
            CreateScene("MainMenu", true);
            CreateScene("Level01", false);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneDirectory + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(SceneDirectory + "/Level01.unity", true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateScene(string name, bool menu)
        {
            string path = SceneDirectory + "/" + name + ".unity";
            if (File.Exists(path)) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject bootstrap = new GameObject("GameBootstrap");
            bootstrap.AddComponent<GameBootstrap>();
            new GameObject(menu ? "MainMenuPresenter" : "Level01Presenter").AddComponent<SceneMarker>();
            EditorSceneManager.SaveScene(scene, path);
        }
    }

}
