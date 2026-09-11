using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    public sealed class SceneUIRegistrar : MonoBehaviour
    {
        [SerializeField] private UINodeBase _node;
        [SerializeField] private UIManager _manager;
        [SerializeField] private bool _closeOnRegister = true;
        public int UniqueId { get; private set; } = -1;
        private void Reset() => _node = GetComponent<UINodeBase>();
        private void Start() => Register();
        public void Register(UIManager manager = null)
        {
            if (UniqueId != -1) return;
            _node = _node != null ? _node : GetComponentInChildren<UINodeBase>(true);
            if (_node == null) throw new System.InvalidOperationException($"{name}: missing UI node.");
            _manager = manager != null ? manager : (_manager != null ? _manager : UIManager.Instance);
            UniqueId = _manager.RegisterSceneNode(_node, _closeOnRegister);
        }
        private void OnDestroy()
        {
            if (_manager != null) _manager.UnregisterSceneNode(_node);
        }
    }
}
