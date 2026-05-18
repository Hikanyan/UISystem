#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HikanyanLibrary.Tool
{
    public class PrefabKeysAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!PrefabKeysGeneratorSettings.AutoGenerateOnModified) return;

            string watchPath = PrefabKeysGeneratorSettings.FilterPath;
            if (string.IsNullOrEmpty(watchPath)) return;

            bool shouldExecute = false;
            var allChanges = importedAssets.Concat(deletedAssets).Concat(movedAssets);

            foreach (var assetPath in allChanges)
            {
                if (assetPath.EndsWith(".prefab") && assetPath.StartsWith(watchPath, System.StringComparison.OrdinalIgnoreCase))
                {
                    shouldExecute = true;
                    break;
                }
            }

            if (shouldExecute)
            {
                EditorApplication.delayCall += () =>
                {
                    AddressableAssetsUtil.MoveSubEntryToRootAndGenerateKeys();
                };
            }
        }
    }
}
#endif