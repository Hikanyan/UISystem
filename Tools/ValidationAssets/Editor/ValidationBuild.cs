using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets;
using UnityEngine;

[InitializeOnLoad]
public static class ValidationBuild
{
    static ValidationBuild()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool("UISystemValidationPrepared", false))
            {
                SessionState.SetBool("UISystemValidationPrepared", true);
                Prepare();
            }
        };
    }
    public static void Prepare()
    {
        AddressableAssetSettingsDefaultObject.GetSettings(true);
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input = settings.FindProperty("activeInputHandler");
        if (input != null) { input.intValue = 2; settings.ApplyModifiedPropertiesWithoutUndo(); }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Minimal/Minimal.unity", true) };
        AssetDatabase.SaveAssets();
    }
    public static void Build()
    {
        var output = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Build/",
            Application.platform == RuntimePlatform.LinuxEditor ? "UISystem.x86_64" : "UISystem.exe"));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Minimal/Minimal.unity" }, locationPathName = output,
            target = Application.platform == RuntimePlatform.LinuxEditor ? BuildTarget.StandaloneLinux64 : BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception($"Build failed: {report.summary.result}");
    }
}
