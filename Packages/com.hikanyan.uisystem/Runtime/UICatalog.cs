using System;
using System.Collections.Generic;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    [CreateAssetMenu(menuName = "UI/Catalog", fileName = "UICatalog")]
    public sealed class UICatalog : ScriptableObject
    {
        public List<UIDefinition> Screens = new();
        public UIDefinition Get(string id)
        {
            UIDefinition result = null;
            foreach (var screen in Screens)
            {
                if (screen == null || screen.Id != id) continue;
                if (result != null) throw new InvalidOperationException($"Duplicate UI id: {id}.");
                result = screen;
            }
            if (result == null) throw new KeyNotFoundException($"UI '{id}' is not in catalog '{name}'.");
            return result;
        }
        public IEnumerable<string> Validate()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var screen in Screens)
            {
                if (screen == null) { yield return "Catalog contains an empty entry."; continue; }
                if (string.IsNullOrWhiteSpace(screen.Id)) yield return $"{screen.name}: empty id.";
                else if (!ids.Add(screen.Id)) yield return $"Duplicate id: {screen.Id}.";
                if (screen.Prefab == null && string.IsNullOrWhiteSpace(screen.AddressableKey)) yield return $"{screen.Id}: missing prefab / address.";
                if (screen.Prefab != null && screen.Prefab.transform is not RectTransform) yield return $"{screen.Id}: prefab requires RectTransform.";
            }
        }
    }
}
