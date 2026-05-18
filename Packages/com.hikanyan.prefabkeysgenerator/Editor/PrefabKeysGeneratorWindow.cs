#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace HikanyanLibrary.Tool
{
    public class PrefabKeysGeneratorWindow : EditorWindow
    {
        private string _outputPath;
        private bool _autoGenerateOnModified;
        private string _namespaceValue;
        private string _generatedKeyCount;
        private string _filterPath;
        private string _targetGroupName;

        [MenuItem("HikanyanLaboratory/Prefab Keys Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefabKeysGeneratorWindow>("Prefab Keys Tool");
            window.minSize = new Vector2(350, 480);
        }

        private void OnEnable() => LoadSettings();

        private void LoadSettings()
        {
            _outputPath = PrefabKeysGeneratorSettings.OutputPath;
            _autoGenerateOnModified = PrefabKeysGeneratorSettings.AutoGenerateOnModified;
            _namespaceValue = PrefabKeysGeneratorSettings.Namespace;
            _filterPath = PrefabKeysGeneratorSettings.FilterPath;
            _targetGroupName = PrefabKeysGeneratorSettings.TargetGroupName;
        }

        private void OnGUI()
        {
            GUILayout.Label("PrefabKeys Generator Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 1. Output Settings
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Output Settings", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Output Path", EditorStyles.miniLabel);
                var newOutputPath = EditorGUILayout.TextField(_outputPath);
                if (newOutputPath != _outputPath)
                {
                    _outputPath = newOutputPath;
                    PrefabKeysGeneratorSettings.OutputPath = _outputPath;
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Namespace", EditorStyles.miniLabel);
                var newNamespace = EditorGUILayout.TextField(_namespaceValue);
                if (newNamespace != _namespaceValue)
                {
                    _namespaceValue = newNamespace;
                    PrefabKeysGeneratorSettings.Namespace = _namespaceValue;
                }
            }

            EditorGUILayout.Space();

            // 2. Addressable Settings
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Addressable & Filter Settings", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Watch Prefab Path", EditorStyles.miniLabel);
                var newFilterPath = EditorGUILayout.TextField(_filterPath);
                if (newFilterPath != _filterPath)
                {
                    _filterPath = newFilterPath;
                    PrefabKeysGeneratorSettings.FilterPath = _filterPath;
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Target Addressable Group", EditorStyles.miniLabel);
                var aaSettings = AddressableAssetSettingsDefaultObject.Settings;
                if (aaSettings != null)
                {
                    var groupNames = aaSettings.groups.Where(g => g != null).Select(g => g.Name).ToArray();
                    int currentIndex = System.Array.IndexOf(groupNames, _targetGroupName);
                    if (currentIndex == -1) currentIndex = 0;

                    int newIndex = EditorGUILayout.Popup(currentIndex, groupNames);
                    if (newIndex != currentIndex || string.IsNullOrEmpty(_targetGroupName))
                    {
                        _targetGroupName = groupNames[newIndex];
                        PrefabKeysGeneratorSettings.TargetGroupName = _targetGroupName;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Addressables が初期化されていません。", MessageType.Error);
                }
            }

            EditorGUILayout.Space();

            // 3. Auto
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Auto Generation", EditorStyles.boldLabel);
                var newAutoGenerate = EditorGUILayout.Toggle("Auto Generate on Modified", _autoGenerateOnModified);
                if (newAutoGenerate != _autoGenerateOnModified)
                {
                    _autoGenerateOnModified = newAutoGenerate;
                    PrefabKeysGeneratorSettings.AutoGenerateOnModified = _autoGenerateOnModified;
                }
            }

            EditorGUILayout.Space();

            // 4. Actions
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button("Execute Process (All-in-One)", GUILayout.Height(40)))
                {
                    GenerateNow();
                }

                EditorGUILayout.Space(5);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Reset Settings"))
                    {
                        if (EditorUtility.DisplayDialog("Reset", "設定を戻しますか？", "Yes", "No"))
                        {
                            PrefabKeysGeneratorSettings.ResetToDefault();
                            LoadSettings();
                        }
                    }
                    if (GUILayout.Button("Open Folder"))
                    {
                        var directory = System.IO.Path.GetDirectoryName(_outputPath);
                        if (System.IO.Directory.Exists(directory)) EditorUtility.RevealInFinder(directory);
                    }
                }
            }

            if (!string.IsNullOrEmpty(_generatedKeyCount))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(_generatedKeyCount, MessageType.Info);
            }
        }

        private void GenerateNow()
        {
            if (!AddressableAssetsUtil.IsAddressablesInitialized())
            {
                _generatedKeyCount = "✗ Addressables が初期化されていません。";
                return;
            }
            int count = AddressableAssetsUtil.MoveSubEntryToRootAndGenerateKeys(_outputPath, _namespaceValue, _filterPath);
            _generatedKeyCount = count > 0 ? $"✓ 成功: {count} 個のキーを生成・同期しました。" : "⚠ 対象のPrefabが見つからないか、0件です。";
        }
    }
}
#endif