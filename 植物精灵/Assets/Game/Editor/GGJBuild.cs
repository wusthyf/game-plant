using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace PlantSpirit.GGJ.Editor
{
    public static class GGJBuild
    {
        public static void BuildWindows64()
        {
            Build(BuildOptions.CleanBuildCache);
        }

        public static void BuildWindows64Development()
        {
            Build(BuildOptions.Development | BuildOptions.CleanBuildCache);
        }

        private static void Build(BuildOptions buildOptions)
        {
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Game/Scenes/MainMenu.unity", "Assets/Game/Scenes/Level01.unity" },
                locationPathName = "E:/26翌光游戏开发/植物精灵/Build/Windows/PlantSpirit.exe",
                target = BuildTarget.StandaloneWindows64,
                options = buildOptions
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Windows build failed: " + report.summary.result);
        }
    }
}
