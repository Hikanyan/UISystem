#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace HikanyanLibrary.Tool
{
    public static class PrefabBinderInitializer
    {
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            AddressableAssetSettings.OnModificationGlobal -= OnAddressablesModified;
            AddressableAssetSettings.OnModificationGlobal += OnAddressablesModified;
        }

        private static void OnAddressablesModified(AddressableAssetSettings settings,
            AddressableAssetSettings.ModificationEvent evt, object obj)
        {
            if (evt != AddressableAssetSettings.ModificationEvent.EntryAdded &&
                evt != AddressableAssetSettings.ModificationEvent.EntryRemoved &&
                evt != AddressableAssetSettings.ModificationEvent.EntryModified) return;
            Debug.Log("Addressables Modified, Regenerating PrefabKeys...");
            AddressableAssetsUtil.MoveSubEntryToRootAndGenerateKeys();
        }
    }
}
#endif