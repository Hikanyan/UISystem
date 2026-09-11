#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace HikanyanLibrary.Tool
{
    public static class AddressableAssetsUtil
    {
        private static bool _running;
        private static bool _scheduled;
        public static bool IsAddressablesInitialized() => AddressableAssetSettingsDefaultObject.Settings != null;
        [MenuItem("HikanyanLaboratory/Addressable/Register and Generate Keys")]
        public static void RegisterAndGenerateKeysMenu() => MoveSubEntryToRootAndGenerateKeys();
        public static void ScheduleGeneration()
        {
            if (_running || _scheduled || !PrefabKeysGeneratorSettings.AutoGenerateOnModified) return;
            _scheduled = true;
            EditorApplication.delayCall += () =>
            {
                _scheduled = false;
                if (!PrefabKeysGeneratorSettings.AutoGenerateOnModified) return;
                try { MoveSubEntryToRootAndGenerateKeys(); }
                catch (Exception e) { UnityEngine.Debug.LogError($"PrefabKeys: {e.Message}"); }
            };
        }
        public static int MoveSubEntryToRootAndGenerateKeys(string outputPath = null, string @namespace = null, string filterPath = null)
        {
            if (_running) return 0;
            _running = true;
            try
            {
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null) throw new InvalidOperationException("Initialize Addressables before generating keys.");
                var folder = (filterPath ?? PrefabKeysGeneratorSettings.FilterPath).Replace('\\', '/').TrimEnd('/');
                if (!folder.StartsWith("Assets/", StringComparison.Ordinal) || !AssetDatabase.IsValidFolder(folder))
                    throw new ArgumentException("Select an existing UI subfolder under Assets. Assets-wide registration is disabled.");
                var path = (outputPath ?? PrefabKeysGeneratorSettings.OutputPath).Replace('\\', '/');
                var fullPath = Path.GetFullPath(path);
                if (!fullPath.StartsWith(Path.GetFullPath("Assets") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Output must be a .cs file inside Assets.");
                var ns = @namespace ?? PrefabKeysGeneratorSettings.Namespace;
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                var keys = new Dictionary<string, string>(StringComparer.Ordinal);
                var addresses = new HashSet<string>(StringComparer.Ordinal);
                // Validate everything before modifying Addressables or files.
                foreach (var guid in guids)
                {
                    var name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                    var entry = settings.FindAssetEntry(guid);
                    var address = entry?.address ?? guid;
                    if (!keys.TryAdd(name, address)) throw new InvalidOperationException($"Duplicate prefab name: {name}. Rename one prefab.");
                    if (!addresses.Add(address)) throw new InvalidOperationException($"Duplicate address: {address}.");
                    foreach (var group in settings.groups)
                        if (group != null && group.entries.Any(e => e.guid != guid && e.address == address))
                            throw new InvalidOperationException($"Address '{address}' is used by another asset.");
                }
                var content = BuildSource(keys, ns);
                var target = settings.groups.FirstOrDefault(g => g != null && g.Name == PrefabKeysGeneratorSettings.TargetGroupName) ?? settings.DefaultGroup;
                if (target == null) throw new InvalidOperationException("Select an Addressables group.");
                foreach (var guid in guids)
                {
                    if (settings.FindAssetEntry(guid) != null) continue;
                    settings.CreateOrMoveEntry(guid, target).SetAddress(guid);
                }
                if (!File.Exists(path) || File.ReadAllText(path) != content)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, content, new UTF8Encoding(false));
                    AssetDatabase.ImportAsset(path);
                }
                AssetDatabase.SaveAssets();
                return keys.Count;
            }
            finally { _running = false; }
        }
        public static string BuildSource(IReadOnlyDictionary<string, string> keys, string ns)
        {
            string Identifier(string value)
            {
                if (string.IsNullOrEmpty(value) || !Regex.IsMatch(value, @"^[\p{L}_][\p{L}\p{Nd}_]*$"))
                    throw new ArgumentException($"Invalid C# identifier: '{value}'. Rename the asset or namespace.");
                return "@" + value;
            }
            string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
            var result = new StringBuilder("// <auto-generated />\n");
            if (!string.IsNullOrEmpty(ns)) result.Append("namespace ").Append(string.Join(".", ns.Split('.').Select(Identifier))).Append("\n{\n");
            result.Append("public static class PrefabKeys\n{\n");
            foreach (var pair in keys.OrderBy(x => x.Key, StringComparer.Ordinal))
                result.Append("    public const string ").Append(Identifier(pair.Key)).Append(" = \"").Append(Escape(pair.Value)).Append("\";\n");
            result.Append("}\n");
            if (!string.IsNullOrEmpty(ns)) result.Append("}\n");
            return result.ToString();
        }
    }
}
#endif
