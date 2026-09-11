using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    public sealed class UIHandle
    {
        private readonly UIManager _manager;
        private readonly UniTaskCompletionSource _closed = new();
        public UINodeBase Node { get; }
        public UIDefinition Definition { get; }
        public bool IsClosed { get; private set; }
        public UniTask Closed => _closed.Task;
        internal GameObject Frame;
        internal CanvasGroup Group;
        internal GameObject PreviousSelection;
        internal IUIViewLoader Loader;
        internal UIHandle(UIManager manager, UINodeBase node, UIDefinition definition)
        { _manager = manager; Node = node; Definition = definition; }
        public UniTask CloseAsync(CancellationToken token = default) => IsClosed || _manager == null ? UniTask.CompletedTask : _manager.CloseHandleAsync(this, token);
        internal void Complete()
        {
            if (IsClosed) return;
            IsClosed = true;
            _closed.TrySetResult();
        }
    }
}
