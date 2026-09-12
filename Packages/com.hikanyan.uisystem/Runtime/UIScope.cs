using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    /// <summary>Place on an active scene root to register inactive children, and end their lifetime together.</summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class UIScope : MonoBehaviour
    {
        [SerializeField] private UIManager _manager;
        private SceneUIRegistrar[] _registrars;
        public void Initialize(UIManager manager) => _manager = manager;
        private void Start()
        {
            if (_manager == null) _manager = UIManager.Instance;
            _registrars = GetComponentsInChildren<SceneUIRegistrar>(true);
            foreach (var registrar in _registrars) registrar.Register(_manager);
        }
        private void OnDestroy()
        {
            if (_manager == null || _registrars == null) return;
            foreach (var registrar in _registrars)
                if (registrar != null && _manager.TryGetNode(registrar.UniqueId, out var node)) _manager.UnregisterSceneNode(node);
        }
    }
}
