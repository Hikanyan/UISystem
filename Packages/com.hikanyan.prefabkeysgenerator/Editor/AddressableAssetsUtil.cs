#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace HikanyanLibrary.Tool
{
    public static class AddressableAssetsUtil
    {
        [MenuItem("HikanyanLaboratory/Addressable/Register and Generate Keys")]
        public static void RegisterAndGenerateKeysMenu()
        {
            MoveSubEntryToRootAndGenerateKeys();
        }

        public static bool IsAddressablesInitialized()
        {
            return AddressableAssetSettingsDefaultObject.Settings != null;
        }

        public static int MoveSubEntryToRootAndGenerateKeys(string outputPath = null, string @namespace = null, string filterPath = null)
        {
            var aaSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (aaSettings == null)
            {
                Debug.LogError("AddressableAssetSettings が見つかりません。Addressablesウィンドウから初期化してください。");
                return 0;
            }

            var targetFolder = filterPath ?? PrefabKeysGeneratorSettings.FilterPath;
            var normalizedFilterPath = NormalizePath(targetFolder);

            // 1. 自動登録
            RegisterPrefabsToAddressables(aaSettings, normalizedFilterPath);

            if (aaSettings.groups == null || aaSettings.groups.Count == 0) return 0;

            var prefabKeyDict = new Dictionary<string, string>();

            // 2. 正規化とキー収集
            foreach (var group in aaSettings.groups)
            {
                if (group == null) continue;

                var entries = group.entries.ToList();
                bool groupModified = false;

                foreach (var entry in entries)
                {
                    if (entry == null || !entry.AssetPath.EndsWith(".prefab")) continue;

                    var entryPath = entry.AssetPath.Replace("\\", "/");
                    if (!entryPath.StartsWith(normalizedFilterPath, System.StringComparison.OrdinalIgnoreCase)) continue;

                    var shortName = Path.GetFileNameWithoutExtension(entry.AssetPath);
                    if (string.IsNullOrEmpty(shortName)) continue;

                    if (entry.address != shortName)
                    {
                        entry.SetAddress(shortName);
                        groupModified = true;
                    }

                    if (!prefabKeyDict.ContainsKey(shortName))
                    {
                        prefabKeyDict.Add(shortName, shortName);
                    }
                }

                if (groupModified) EditorUtility.SetDirty(group);
            }

            // 3. C#定数生成
            var finalOutputPath = !string.IsNullOrEmpty(outputPath) ? outputPath : PrefabKeysGeneratorSettings.OutputPath;
            var finalNamespace = @namespace ?? PrefabKeysGeneratorSettings.Namespace;
            
            GeneratePrefabKeysClass(prefabKeyDict, finalOutputPath, finalNamespace);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            return prefabKeyDict.Count;
        }

        private static void RegisterPrefabsToAddressables(AddressableAssetSettings settings, string folderPath)
        {
            string searchPath = folderPath.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(searchPath)) return;

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });
            var targetGroup = settings.groups.FirstOrDefault(g => g != null && g.Name == PrefabKeysGeneratorSettings.TargetGroupName);
            if (targetGroup == null) targetGroup = settings.DefaultGroup;

            if (targetGroup == null) return;

            bool isModified = false;

            foreach (var guid in prefabGuids)
            {
                var existingEntry = settings.FindAssetEntry(guid);
                if (existingEntry == null)
                {
                    var entry = settings.CreateOrMoveEntry(guid, targetGroup);
                    if (entry != null)
                    {
                        Debug.Log($"[PrefabKeys] Addressableに自動追加: {AssetDatabase.GUIDToAssetPath(guid)} -> {targetGroup.Name}");
                        isModified = true;
                    }
                }
            }

            if (isModified) settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryAdded, targetGroup, true);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "Assets/";
            path = path.Replace("\\", "/").Trim();
            if (!path.EndsWith("/")) path += "/";
            return path;
        }

        private static void GeneratePrefabKeysClass(Dictionary<string, string> keyMap, string classPath, string @namespace)
        {
            var content = "////////////////////////////////////////////////////////////\n";
            content += "// <auto-generated>\n";
            content += "//     This code was generated by PrefabKeysGenerator.\n";
            content += "// </auto-generated>\n";
            content += "////////////////////////////////////////////////////////////\n\n";

            if (!string.IsNullOrEmpty(@namespace)) content += $"namespace {@namespace}\n{{\n";

            var indent = string.IsNullOrEmpty(@namespace) ? "" : "    ";
            content += $"{indent}public static class PrefabKeys\n{indent}{{\n";
            
            foreach (var kvp in keyMap.OrderBy(x => x.Key))
            {
                if (!IsValidIdentifier(kvp.Key)) continue;
                content += $"{indent}    public const string {kvp.Key} = \"{kvp.Value}\";\n";
            }

            content += $"{indent}}}\n";
            if (!string.IsNullOrEmpty(@namespace)) content += "}";

            try
            {
                var directory = Path.GetDirectoryName(classPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(classPath, content);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PrefabKeys書き込み失敗: {ex.Message}");
                throw;
            }
        }

        private static bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return false;
            if (!char.IsLetter(identifier[0]) && identifier[0] != '_') return false;
            return identifier.Skip(1).All(c => char.IsLetterOrDigit(c) || c == '_');
        }
    }
}
#endif