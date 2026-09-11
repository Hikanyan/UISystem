using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    [DefaultExecutionOrder(-10000)]
    public sealed partial class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance => _instance != null ? _instance :
            throw new InvalidOperationException("Add a UIManager / UI Bootstrap before opening UI.");
        public static bool TryGetInstance(out UIManager manager) { manager = _instance; return manager != null; }
        [SerializeField] private Transform _defaultRoot;
        [SerializeField] private bool _persistent = true;
        private readonly CancellationTokenSource _lifetime = new();
        private readonly Dictionary<int, NodeEntry> _nodes = new();
        private readonly Dictionary<Type, int> _sceneTypeToId = new();
        public int RegisteredCount => _nodes.Count;
        public Transform DefaultRoot { get => _defaultRoot; set => _defaultRoot = value; }

        private sealed class NodeEntry
        {
            public UINodeBase Node;
            public bool Generated;
        }

        private void Awake()
        {
            // Additional managers are valid explicit scopes; the first is the convenience default.
            if (_instance == null) _instance = this;
            if (_persistent && transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        public int RegisterSceneNode(UINodeBase node, bool closeOnRegister = true)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (node.State == UIState.Disposed) throw new ObjectDisposedException(node.name);
            var id = node.gameObject.GetInstanceID();
            if (_nodes.TryGetValue(id, out var same))
            {
                if (same.Node != node || same.Generated) throw new InvalidOperationException("Node is already registered with another lifetime.");
                return id;
            }
            var type = node.GetType();
            if (_sceneTypeToId.TryGetValue(type, out var other) && _nodes.TryGetValue(other, out var entry) && entry.Node != null)
                throw new InvalidOperationException($"Duplicate scene UI type {type.Name}. Use a separate UIManager scope.");
            node.InternalSetup(id, this);
            node.InternalSetInitialState(!closeOnRegister && node.gameObject.activeSelf, !closeOnRegister && node.gameObject.activeSelf);
            _nodes.Add(id, new NodeEntry { Node = node });
            _sceneTypeToId[type] = id;
            node.Disposed += OnNodeDisposed;
            return id;
        }

        public void UnregisterSceneNode(UINodeBase node)
        {
            if (ReferenceEquals(node, null)) return;
            if (_nodes.TryGetValue(node.UniqueId, out var entry) && !entry.Generated && entry.Node == node)
            {
                RemoveEntry(node.UniqueId);
                node.Dispose();
            }
        }

        private void OnNodeDisposed(UINodeBase node) => RemoveEntry(node.UniqueId);
        private void RemoveEntry(int id)
        {
            if (!_nodes.TryGetValue(id, out var entry)) return;
            _nodes.Remove(id);
            if (!ReferenceEquals(entry.Node, null))
            {
                entry.Node.Disposed -= OnNodeDisposed;
                var type = entry.Node.GetType();
                if (_sceneTypeToId.TryGetValue(type, out var registered) && registered == id) _sceneTypeToId.Remove(type);
            }
        }

        public async UniTask<int> OpenAsync<TPresenter, TParam>(string prefabKey, TParam parameter,
            Transform parent = null, CancellationToken cancellationToken = default)
            where TPresenter : UINodeBase where TParam : Parameter
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
            var presenter = await AddressablePrefabLoader.LoadAndInstantiateAsync<TPresenter>(prefabKey,
                parent != null ? parent : _defaultRoot, linked.Token);
            return await OpenGeneratedAsync(presenter, parameter, linked.Token);
        }

        private async UniTask<int> OpenGeneratedAsync(UINodeBase node, Parameter parameter, CancellationToken token)
        {
            var id = node.gameObject.GetInstanceID();
            try
            {
                token.ThrowIfCancellationRequested();
                node.InternalSetup(id, this);
                _nodes.Add(id, new NodeEntry { Node = node, Generated = true });
                node.Disposed += OnNodeDisposed;
                await node.OpenAsync(parameter, token);
                return id;
            }
            catch { ReleaseGenerated(id, node); throw; }
        }

        private void ReleaseGenerated(int id, UINodeBase node)
        {
            RemoveEntry(id);
            if (node == null) return;
            new DefaultUIViewLoader().Release(node);
        }

        public async UniTask CloseAsync(int uniqueId, CancellationToken cancellationToken = default)
        {
            if (_handles.TryGetValue(uniqueId, out var handle)) { await CloseHandleAsync(handle, cancellationToken); return; }
            cancellationToken.ThrowIfCancellationRequested();
            if (!_nodes.TryGetValue(uniqueId, out var entry) || entry.Node == null) return;
            try { await entry.Node.CloseAsync(cancellationToken); }
            finally
            {
                if (entry.Generated && (entry.Node == null || entry.Node.State == UIState.Closed || entry.Node.State == UIState.Disposed))
                    ReleaseGenerated(uniqueId, entry.Node);
            }
        }

        public async UniTask OpenSceneAsync<TPresenter, TParam>(TParam parameter, CancellationToken cancellationToken = default)
            where TPresenter : UINodeBase where TParam : Parameter
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
            var deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (!_sceneTypeToId.ContainsKey(typeof(TPresenter)))
            {
                linked.Token.ThrowIfCancellationRequested();
                if (Time.realtimeSinceStartupAsDouble >= deadline)
                    throw new TimeoutException($"Scene UI {typeof(TPresenter).Name} was not registered. Check SceneUIRegistrar / UIScope.");
                await UniTask.Yield(PlayerLoopTiming.Update, linked.Token);
            }
            var id = _sceneTypeToId[typeof(TPresenter)];
            if (!TryGetNode(id, out var node)) throw new InvalidOperationException("The scene UI scope has ended.");
            await node.OpenAsync(parameter, linked.Token);
        }

        public UniTask CloseSceneAsync<TPresenter>(CancellationToken cancellationToken = default) where TPresenter : UINodeBase =>
            _sceneTypeToId.TryGetValue(typeof(TPresenter), out var id) ? CloseAsync(id, cancellationToken) : UniTask.CompletedTask;

        public bool TryGetNode(int id, out UINodeBase node)
        {
            node = _nodes.TryGetValue(id, out var entry) ? entry.Node : null;
            return node != null;
        }

        private void OnDestroy()
        {
            _lifetime.Cancel();
            DisposePresentations();
            var entries = new List<NodeEntry>(_nodes.Values);
            foreach (var entry in entries)
            {
                if (entry.Node == null) continue;
                entry.Node.Dispose();
                if (entry.Generated) Destroy(entry.Node.gameObject);
            }
            _nodes.Clear();
            _sceneTypeToId.Clear();
            if (_instance == this) _instance = null;
        }
    }
}
