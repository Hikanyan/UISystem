using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using HikanyanLibrary.Core;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    public sealed class UIManager : SingletonMonoBehaviour<UIManager>
    {
        [SerializeField] private Transform _defaultRoot;
        protected override bool IsPersistent => true;
        private enum NodeLifetime
        {
            Generated,
            SceneResident
        }

        private sealed class NodeEntry
        {
            public UINodeBase Node;
            public NodeLifetime Lifetime;
        }

        // uniqueId -> node
        private readonly Dictionary<int, NodeEntry> _nodes = new();

        // Scene 常駐: Presenter 型 -> uniqueId（同一型は1つ想定）
        private readonly Dictionary<Type, int> _sceneTypeToId = new();

        /// <summary>
        /// Sceneに配置済みのUIを登録する（破棄しない管理）
        /// FixedId は不要。実行中一意な GetInstanceID を採用。
        /// </summary>
        public int RegisterSceneNode(UINodeBase node, bool closeOnRegister = true)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            var id = node.gameObject.GetInstanceID();

            if (_nodes.TryGetValue(id, out var existing) && existing.Node != node)
            {
                throw new InvalidOperationException($"UIManager: InstanceID が衝突しました（通常は起きません）。id={id}");
            }

            node.InternalSetup(id, this);

            // 初期状態は「登録時に閉じる」を推奨（IsOpen=false のままにできるため）
            if (closeOnRegister)
            {
                node.gameObject.SetActive(false);
                // IsOpen は UINodeBase の初期値 false のまま（現状設計と整合） :contentReference[oaicite:3]{index=3}
            }

            _nodes[id] = new NodeEntry { Node = node, Lifetime = NodeLifetime.SceneResident };

            // 型で引けるように登録（同一型複数が必要ならここを拡張）
            var type = node.GetType();
            if (_sceneTypeToId.TryGetValue(type, out var already) && already != id)
            {
                Debug.LogError($"UIManager: SceneResident の同一型が複数登録されました。type={type.Name} id={already} newId={id}");
            }
            else
            {
                _sceneTypeToId[type] = id;
            }

            return id;
        }

        /// <summary>
        /// UI を開く（Addressables から生成）
        /// </summary>
        public async UniTask<int> OpenAsync<TPresenter, TParam>(
            string prefabKey,
            TParam parameter,
            Transform parent = null,
            CancellationToken cancellationToken = default)
            where TPresenter : UINodeBase
            where TParam : Parameter
        {
            cancellationToken.ThrowIfCancellationRequested();

            var root = parent != null ? parent : _defaultRoot;

            TPresenter presenter = root != null
                ? await AddressablePrefabLoader.LoadAndInstantiateAsync<TPresenter>(prefabKey, root, cancellationToken)
                : await AddressablePrefabLoader.LoadAndInstantiateAsync<TPresenter>(prefabKey, cancellationToken);

            if (presenter == null)
                throw new Exception($"UIManager: prefabKey={prefabKey} から {typeof(TPresenter).Name} を取得できませんでした。");

            // GetHashCode() はやめる（衝突し得る） :contentReference[oaicite:4]{index=4}
            var id = presenter.gameObject.GetInstanceID();
            presenter.InternalSetup(id, this);
            _nodes[id] = new NodeEntry { Node = presenter, Lifetime = NodeLifetime.Generated };

            await presenter.OpenAsync(parameter, cancellationToken);
            return id;
        }

        /// <summary>
        /// uniqueId 指定で閉じる
        /// - Generated: Close後にDestroy
        /// - SceneResident: Close後も保持（Destroyしない）
        /// </summary>
        public async UniTask CloseAsync(int uniqueId, CancellationToken cancellationToken)
        {
            if (!_nodes.TryGetValue(uniqueId, out var entry) || entry.Node == null)
                return;

            cancellationToken.ThrowIfCancellationRequested();

            await entry.Node.CloseAsync(cancellationToken);

            if (entry.Lifetime == NodeLifetime.Generated)
            {
                _nodes.Remove(uniqueId);
                Destroy(entry.Node.gameObject); // 現状は常にDestroyしている :contentReference[oaicite:5]{index=5}
            }
        }

        /// <summary>
        /// Scene 常駐UIを「型」で開く（IDを持ち回らなくてよい）
        /// </summary>
        public async UniTask OpenSceneAsync<TPresenter, TParam>(TParam parameter, CancellationToken cancellationToken = default)
            where TPresenter : UINodeBase
            where TParam : Parameter
        {
            // まず登録を待つ（タイムアウト付き）
            var presenterType = typeof(TPresenter);

            if (!_sceneTypeToId.TryGetValue(presenterType, out var id))
            {
                const int timeoutMs = 5000;

                var waitRegistered = UniTask.WaitUntil(
                    () => _sceneTypeToId.ContainsKey(presenterType),
                    cancellationToken: cancellationToken);

                var timeout = UniTask.Delay(timeoutMs, cancellationToken: cancellationToken);

                var winner = await UniTask.WhenAny(waitRegistered, timeout);
                if (winner == 1) // timeout
                    throw new Exception($"UIManager: SceneResident {presenterType.Name} の登録待ちがタイムアウトしました（{timeoutMs}ms）。SceneUIRegistrar の配置/有効状態/実行順を確認してください。");

                id = _sceneTypeToId[presenterType];
            }

            if (!_nodes.TryGetValue(id, out var entry) || entry.Node == null)
                throw new Exception($"UIManager: 登録済みノードが見つかりません。type={presenterType.Name}");

            await entry.Node.OpenAsync(parameter, cancellationToken);
        }


        public async UniTask CloseSceneAsync<TPresenter>(CancellationToken cancellationToken = default)
            where TPresenter : UINodeBase
        {
            if (!_sceneTypeToId.TryGetValue(typeof(TPresenter), out var id))
                return;

            await CloseAsync(id, cancellationToken);
        }

        public bool TryGetNode(int uniqueId, out UINodeBase node)
        {
            if (_nodes.TryGetValue(uniqueId, out var entry))
            {
                node = entry.Node;
                return node != null;
            }

            node = null;
            return false;
        }
    }
}
