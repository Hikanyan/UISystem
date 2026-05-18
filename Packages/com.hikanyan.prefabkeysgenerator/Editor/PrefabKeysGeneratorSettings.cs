#if UNITY_EDITOR
using UnityEditor;

namespace HikanyanLibrary.Tool
{
    public static class PrefabKeysGeneratorSettings
    {
        private const string OutputPathKey = "PrefabKeysGenerator_OutputPath";
        private const string AutoGenerateKey = "PrefabKeysGenerator_AutoGenerate";
        private const string NamespaceKey = "PrefabKeysGenerator_Namespace";
        private const string FilterPathKey = "PrefabKeysGenerator_FilterPath";
        private const string TargetGroupKey = "PrefabKeysGenerator_TargetGroup";

        // 汎用的に使い回せるように初期デフォルトパスをAssets直下に設定
        private const string DefaultOutputPath = "Assets/Generated/PrefabKeys.cs";
        private const string DefaultFilterPath = "Assets";

        public static string OutputPath
        {
            get => EditorPrefs.GetString(OutputPathKey, DefaultOutputPath);
            set => EditorPrefs.SetString(OutputPathKey, value);
        }

        public static bool AutoGenerateOnModified
        {
            get => EditorPrefs.GetBool(AutoGenerateKey, true);
            set => EditorPrefs.SetBool(AutoGenerateKey, value);
        }

        public static string Namespace
        {
            get => EditorPrefs.GetString(NamespaceKey, "HikanyanLaboratory.Generated");
            set => EditorPrefs.SetString(NamespaceKey, value);
        }

        public static string FilterPath
        {
            get => EditorPrefs.GetString(FilterPathKey, DefaultFilterPath);
            set => EditorPrefs.SetString(FilterPathKey, value);
        }

        public static string TargetGroupName
        {
            get => EditorPrefs.GetString(TargetGroupKey, "");
            set => EditorPrefs.SetString(TargetGroupKey, value);
        }

        public static void ResetToDefault()
        {
            EditorPrefs.DeleteKey(OutputPathKey);
            EditorPrefs.DeleteKey(AutoGenerateKey);
            EditorPrefs.DeleteKey(NamespaceKey);
            EditorPrefs.DeleteKey(FilterPathKey);
            EditorPrefs.DeleteKey(TargetGroupKey);
        }
    }
}
#endif