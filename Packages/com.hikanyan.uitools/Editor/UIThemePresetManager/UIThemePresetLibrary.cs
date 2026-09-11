using System;
using System.Collections.Generic;
using UnityEditor.Presets;
using UnityEngine;

namespace HikanyanLibrary.UITools
{
    [CreateAssetMenu(menuName = "UI/Theme Preset Library", fileName = "UIThemePresetLibrary")]
    public class UIThemePresetLibrary : ScriptableObject
    {
        [Serializable]
        public class Category
        {
            public string name;
            public Texture2D icon;
            public List<Preset> presets = new();
        }

        public List<Category> categories = new();

        public Category GetOrCreate(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) categoryName = "Default";

            foreach (var c in categories)
            {
                if (c != null && c.name == categoryName) return c;
            }

            var created = new Category { name = categoryName };
            categories.Add(created);
            return created;
        }
    }
}