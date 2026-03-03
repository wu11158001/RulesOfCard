using UnityEditor;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using UnityEditor.AddressableAssets.Settings;

public class JenkinsBuild
{
    /// <summary>
    /// 打主包
    /// </summary>
    public static void BuildProject()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        string folderPath = "Builds";
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

        // 檢查結果並退出
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Windows Build Succeeded");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("Windows Build Failed");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// 打資源包
    /// </summary>
    public static void BuildAddressables()
    {
        Debug.Log("[Jenkins] 開始建置 Addressables...");

        AddressableAssetSettings.CleanPlayerContent();
        AddressableAssetSettings.BuildPlayerContent();

        Debug.Log("[Jenkins] Addressables 建置完成！");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
}