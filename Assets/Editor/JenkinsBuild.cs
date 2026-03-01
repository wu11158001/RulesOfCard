using UnityEditor;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class JenkinsBuild
{
    public static void BuildProject()
    {
        // 取得勾選場景
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        // 正確做法：資料夾 + exe 分開
        string folderPath = "Builds/Windows";
        string exePath = Path.Combine(folderPath, "RulesOfCard.exe");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        BuildPlayerOptions buildPlayerOptions = new();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = exePath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Windows Build Succeeded");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("Windows Build Failed");
            EditorApplication.Exit(1);
        }
    }
}