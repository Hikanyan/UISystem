using UnityEngine;
using UnityEngine.UI;

namespace HikanyanLibrary.UITools
{
    [RequireComponent(typeof(Graphic))]
    public class ThemeColorBinder : MonoBehaviour
    {
        [SerializeField] private UIColorRole role;

        private Graphic _graphic;
        private UIThemeProvider _provider;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
        }

        private void OnEnable()
        {
            UIThemeProvider.InstanceChanged += Reconnect;
            Reconnect();
        }

        private void OnDisable()
        {
            UIThemeProvider.InstanceChanged -= Reconnect;
            if (_provider != null) _provider.ThemeChanged -= Apply;
            _provider = null;
        }

        private void Reconnect()
        {
            if (_provider != null) _provider.ThemeChanged -= Apply;
            _provider = UIThemeProvider.Instance;
            if (_provider != null) _provider.ThemeChanged += Apply;
            Apply();
        }

        private void Apply()
        {
            var provider = _provider;
            if (provider == null || provider.CurrentTheme == null) return;

            _graphic.color = provider.CurrentTheme.Get(role);
        }
    }
}
