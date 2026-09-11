using System;
using System.Collections.Generic;
using UnityEngine;

namespace HikanyanLibrary.UITools
{
    [CreateAssetMenu(menuName = "UI/Theme", fileName = "UITheme")]
    public class UITheme : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public UIColorRole role;
            public Color color;
        }

        [SerializeField] private List<Entry> entries = new();

        private Dictionary<UIColorRole, Color> _cache;
        public event Action Changed;
        public void SetColor(UIColorRole role, Color color)
        {
            entries.RemoveAll(entry => entry.role == role);
            entries.Add(new Entry { role = role, color = color });
            Invalidate();
        }
        public void Invalidate() { _cache = null; Changed?.Invoke(); }
        private void OnValidate() => Invalidate();

        public Color Get(UIColorRole role)
        {
            _cache ??= BuildCache();
            return _cache.TryGetValue(role, out var c) ? c : Color.magenta; // 未設定の視認性用
        }

        private Dictionary<UIColorRole, Color> BuildCache()
        {
            var dict = new Dictionary<UIColorRole, Color>();
            foreach (var e in entries) dict[e.role] = e.color;
            return dict;
        }
    }
}
