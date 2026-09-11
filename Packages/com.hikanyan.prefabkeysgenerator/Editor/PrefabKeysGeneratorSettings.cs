using UnityEditor;

namespace HikanyanLibrary.Tool
{
    [FilePath("ProjectSettings/HikanyanPrefabKeys.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class PrefabKeysProjectSettings : ScriptableSingleton<PrefabKeysProjectSettings>
    {
        public string outputPath = "Assets/Generated/PrefabKeys.cs";
        public string filterPath = "Assets/UI";
        public string namespaceName = "Game.UI";
        public string groupName = "";
        public bool autoGenerate;
        public void Persist() => Save(true);
    }
    public static class PrefabKeysGeneratorSettings
    {
        private static PrefabKeysProjectSettings Data => PrefabKeysProjectSettings.instance;
        public static string OutputPath { get => Data.outputPath; set { Data.outputPath = value; Data.Persist(); } }
        public static string FilterPath { get => Data.filterPath; set { Data.filterPath = value; Data.Persist(); } }
        public static string Namespace { get => Data.namespaceName; set { Data.namespaceName = value; Data.Persist(); } }
        public static string TargetGroupName { get => Data.groupName; set { Data.groupName = value; Data.Persist(); } }
        public static bool AutoGenerateOnModified { get => Data.autoGenerate; set { Data.autoGenerate = value; Data.Persist(); } }
        public static void ResetToDefault()
        {
            OutputPath = "Assets/Generated/PrefabKeys.cs";
            FilterPath = "Assets/UI";
            Namespace = "Game.UI";
            TargetGroupName = "";
            AutoGenerateOnModified = false;
        }
    }
}
