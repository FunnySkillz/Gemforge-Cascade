using System;
using System.IO;
using GemforgeCascade.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GemforgeCascade.Editor
{
    public static class BuildVerification
    {
        [MenuItem("Gemforge/Validate Chapter and Build Windows")]
        public static void BuildWindows()
        {
            var files = Directory.GetFiles("Assets/Resources/Levels", "*.json");
            if (files.Length != 10) throw new InvalidOperationException("Expected ten chapter levels.");
            foreach (string file in files)
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(file.Replace('\\', '/'));
                var level = LevelCatalog.Parse(asset);
                Debug.Log($"VALIDATED {level.id}: {LevelValidator.Validate(level).LegalMoveCount} opening moves");
            }
            if (LevelCatalog.LoadAll().Length != files.Length)
                throw new InvalidOperationException("Runtime catalog and authored chapter differ.");
            PlayerSettings.companyName = "FunnySkillz";
            PlayerSettings.productName = "Gemforge Cascade";
            PlayerSettings.defaultScreenWidth = 720;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = "Builds/Windows/GemforgeCascade.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Build failed: {report.summary.result}");
            Debug.Log("GEMFORGE_BUILD_PASS");
        }
    }
}
