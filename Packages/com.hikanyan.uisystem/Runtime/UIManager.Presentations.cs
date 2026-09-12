using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HikanyanLibrary.UISystem
{
    public sealed partial class UIManager
    {
        [SerializeField] private UICatalog _catalog;
        [SerializeField, Min(0)] private int _cacheCapacity = 8;
        private readonly SemaphoreSlim _presentationGate = new(1, 1);
        private readonly Dictionary<int, UIHandle> _handles = new();
        private readonly List<UIHandle> _stack = new();
        private readonly Dictionary<UILayer, Transform> _layers = new();
        private readonly Dictionary<string, CachedView> _cache = new();
        private sealed class CachedView { public UINodeBase Node; public IUIViewLoader Loader; }
        public UICatalog Catalog { get => _catalog; set => _catalog = value; }
        public IUIViewLoader Loader { get; set; } = new DefaultUIViewLoader();
        public int CachedCount => _cache.Count;
        public void SetLayerRoot(UILayer layer, Transform root) => _layers[layer] = root;

        public async UniTask<UIHandle> OpenAsync<TArgs>(UIKey<TArgs> key, TArgs args, CancellationToken cancellationToken = default)
            where TArgs : Parameter
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (_catalog == null) throw new InvalidOperationException("Assign a UICatalog to UIManager.");
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
            await _presentationGate.WaitAsync(linked.Token);
            try
            {
                linked.Token.ThrowIfCancellationRequested();
                var definition = _catalog.Get(key.Id);
                if (definition.Duplicate == UIDuplicate.FocusExisting)
                {
                    foreach (var existing in _stack.ToArray())
                    {
                        if (existing.Definition != definition || existing.IsClosed) continue;
                        if (!key.PresenterType.IsInstanceOfType(existing.Node)) throw new InvalidOperationException("UI key presenter type mismatch.");
                        _stack.Remove(existing);
                        _stack.Add(existing);
                        existing.Frame.transform.SetAsLastSibling();
                        RefreshPresentation();
                        return existing; // Existing arguments are intentionally retained.
                    }
                }
                var root = _layers.TryGetValue(definition.Layer, out var layerRoot) && layerRoot != null ? layerRoot : _defaultRoot;
                var frame = new GameObject(definition.Id, typeof(RectTransform), typeof(CanvasGroup));
                var rect = (RectTransform)frame.transform;
                rect.SetParent(root != null ? root : transform, false);
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                if (definition.Layer == UILayer.Modal)
                {
                    var blocker = frame.AddComponent<Image>();
                    blocker.color = new Color(0, 0, 0, 0.45f);
                    blocker.raycastTarget = true;
                }
                UINodeBase node = null;
                UIHandle handle = null;
                var loader = Loader ?? throw new InvalidOperationException("Assign a view loader.");
                try
                {
                    if (_cache.TryGetValue(definition.Id, out var cached))
                    {
                        _cache.Remove(definition.Id);
                        node = cached.Node;
                        loader = cached.Loader;
                        if (node != null && node.State != UIState.Disposed) node.transform.SetParent(frame.transform, false);
                        else { loader.Release(node); node = null; loader = Loader; }
                    }
                    if (node == null) node = await loader.LoadAsync(definition, frame.transform, linked.Token);
                    linked.Token.ThrowIfCancellationRequested();
                    if (node == null || !key.PresenterType.IsInstanceOfType(node))
                        throw new InvalidOperationException($"UI '{key.Id}' must contain {key.PresenterType.Name}.");
                    var id = node.gameObject.GetInstanceID();
                    node.InternalSetup(id, this);
                    handle = new UIHandle(this, node, definition)
                    {
                        Frame = frame, Group = frame.GetComponent<CanvasGroup>(), Loader = loader,
                        PreviousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null
                    };
                    _nodes.Add(id, new NodeEntry { Node = node, Generated = true });
                    _handles.Add(id, handle);
                    _stack.Add(handle);
                    node.Disposed += OnPresentationDisposed;
                    foreach (var selectable in node.GetComponentsInChildren<Selectable>(true))
                    {
                        var back = selectable.GetComponent<UIBackHandler>() ?? selectable.gameObject.AddComponent<UIBackHandler>();
                        back.Initialize(this);
                    }
                    RefreshPresentation();
                    await node.OpenAsync(args, linked.Token);
                    linked.Token.ThrowIfCancellationRequested();
                    RefreshPresentation();
                    return handle;
                }
                catch
                {
                    if (handle != null) FinishPresentation(handle, false);
                    else { loader.Release(node); Destroy(frame); }
                    throw;
                }
            }
            finally { _presentationGate.Release(); }
        }

        internal async UniTask CloseHandleAsync(UIHandle handle, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (handle.IsClosed) return;
            // Cancel the node's opening before waiting for the manager's operation gate.
            if (handle.Node != null && handle.Node.State == UIState.Opening)
                await handle.Node.CloseAsync(token);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime.Token);
            await _presentationGate.WaitAsync(linked.Token);
            try
            {
                if (handle.IsClosed) return;
                try { if (handle.Node != null) await handle.Node.CloseAsync(linked.Token); }
                finally { FinishPresentation(handle, handle.Definition.Reuse == UIReuse.KeepAlive); }
            }
            finally { _presentationGate.Release(); }
        }

        public async UniTask<TResult> ShowDialogAsync<TArgs, TResult>(UIDialogKey<TArgs, TResult> key, TArgs args,
            CancellationToken cancellationToken = default) where TArgs : Parameter
        {
            var handle = await OpenAsync(key.Screen, args, cancellationToken);
            try { return await ((IUIResult<TResult>)handle.Node).WaitForResultAsync(cancellationToken); }
            finally { await handle.CloseAsync(); }
        }

        public async UniTask<bool> BackAsync(CancellationToken token = default)
        {
            for (var layer = (int)UILayer.Modal; layer >= (int)UILayer.Page; layer--)
                for (var i = _stack.Count - 1; i >= 0; i--)
                {
                    var handle = _stack[i];
                    if ((int)handle.Definition.Layer != layer || handle.IsClosed) continue;
                    if (!handle.Definition.CloseOnBack) return false;
                    await handle.CloseAsync(token);
                    return true;
                }
            return false;
        }

        private void OnPresentationDisposed(UINodeBase node)
        {
            if (_handles.TryGetValue(node.UniqueId, out var handle)) FinishPresentation(handle, false);
        }
        private void FinishPresentation(UIHandle handle, bool keep)
        {
            if (handle.IsClosed) return;
            handle.Complete();
            _stack.Remove(handle);
            if (!ReferenceEquals(handle.Node, null))
            {
                _handles.Remove(handle.Node.UniqueId);
                RemoveEntry(handle.Node.UniqueId);
                handle.Node.Disposed -= OnPresentationDisposed;
            }
            if (keep && handle.Node != null && handle.Node.State == UIState.Closed && _cacheCapacity > 0 && !_lifetime.IsCancellationRequested)
            {
                if (_cache.TryGetValue(handle.Definition.Id, out var old)) old.Loader.Release(old.Node);
                else if (_cache.Count >= _cacheCapacity) ClearCache();
                handle.Node.transform.SetParent(transform, false);
                _cache[handle.Definition.Id] = new CachedView { Node = handle.Node, Loader = handle.Loader };
            }
            else handle.Loader.Release(handle.Node);
            if (handle.Frame != null) { handle.Frame.SetActive(false); Destroy(handle.Frame); }
            RefreshPresentation(handle.PreviousSelection);
        }

        public void ClearCache()
        {
            foreach (var cached in _cache.Values) cached.Loader.Release(cached.Node);
            _cache.Clear();
        }
        private void DisposePresentations()
        {
            foreach (var handle in _stack.ToArray()) FinishPresentation(handle, false);
            ClearCache();
        }
        private void RefreshPresentation(GameObject restoreSelection = null)
        {
            UIHandle modal = null, page = null;
            foreach (var handle in _stack)
            {
                if (handle.Definition.Layer == UILayer.Modal) modal = handle;
                if (handle.Definition.Layer == UILayer.Page) page = handle;
            }
            foreach (var handle in _stack)
            {
                if (handle.Group == null) continue;
                var visible = handle.Definition.Layer != UILayer.Page || handle == page;
                var input = visible && (modal == null || handle == modal) && handle.Definition.Layer != UILayer.Toast;
                handle.Group.alpha = visible ? 1 : 0;
                handle.Group.interactable = input;
                handle.Group.blocksRaycasts = input;
            }
            if (EventSystem.current == null) return;
            var top = modal ?? page;
            if (top == null) { EventSystem.current.SetSelectedGameObject(restoreSelection != null && restoreSelection.activeInHierarchy ? restoreSelection : null); return; }
            var selection = restoreSelection != null ? restoreSelection : EventSystem.current.currentSelectedGameObject;
            if (selection != null && selection.activeInHierarchy && selection.transform.IsChildOf(top.Frame.transform))
            { EventSystem.current.SetSelectedGameObject(selection); return; }
            var selectable = top.Node != null ? top.Node.GetComponentInChildren<Selectable>() : null;
            EventSystem.current.SetSelectedGameObject(selectable != null && selectable.IsInteractable() ? selectable.gameObject : null);
        }
    }
}
