using System;
using UnityEngine;

namespace HikanyanLibrary.UITools
{
    public class UIThemeProvider : MonoBehaviour
    {
        public static UIThemeProvider Instance { get; private set; }
        public static event Action InstanceChanged;

        [SerializeField] private UITheme currentTheme;
        public UITheme CurrentTheme => currentTheme;

        public event Action ThemeChanged;

        private void OnEnable()
        {
            Instance = this;
            if (currentTheme != null) currentTheme.Changed += NotifyThemeChanged;
            InstanceChanged?.Invoke();
        }

        private void OnDisable()
        {
            if (currentTheme != null) currentTheme.Changed -= NotifyThemeChanged;
            if (Instance != this) return;
            Instance = null;
            InstanceChanged?.Invoke();
        }
        private void NotifyThemeChanged() => ThemeChanged?.Invoke();

        public void SetTheme(UITheme theme)
        {
            if (theme == null || theme == currentTheme) return;
            if (currentTheme != null && isActiveAndEnabled) currentTheme.Changed -= NotifyThemeChanged;
            currentTheme = theme;
            if (isActiveAndEnabled) currentTheme.Changed += NotifyThemeChanged;
            ThemeChanged?.Invoke();
        }
    }
}
