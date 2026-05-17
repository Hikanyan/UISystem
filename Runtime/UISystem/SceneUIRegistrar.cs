using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    /// <summary>
    /// Scene 常駐UI（Presenter）を UIManager に登録する
    /// - これ1つを使い回す
    /// - MVP を強制：IPresenterNode で検証
    /// </summary>
    public sealed class SceneUIRegistrar : MonoBehaviour
    {
        [SerializeField] private UINodeBase _node;
        [SerializeField] private bool _closeOnRegister = true;
        [SerializeField] private int _sortOrder = 0; // 未使実装
        
        public int UniqueId { get; private set; } = -1;

        private void Reset()
        {
            _node = GetComponent<UINodeBase>();
        }

        private UINodeBase ResolveNode()
        {
            if (_node != null) return _node;
            _node = GetComponent<UINodeBase>();
            if (_node != null) return _node;
            _node = GetComponentInChildren<UINodeBase>(true);
            return _node;
        }

        private async void Awake()
        {
            await UniTask.WaitUntil(
                () => UIManager.Instance != null,
                cancellationToken: this.GetCancellationTokenOnDestroy());

            var node = ResolveNode();
            if (node == null)
            {
                Debug.LogError($"{nameof(SceneUIRegistrar)}: UINodeBase が見つかりません。");
                return;
            }

            // MVP 強制：PresenterBase 由来であること（= IPresenterNode）を必須にする
            if (node is not IPresenterNode)
            {
                Debug.LogError($"{nameof(SceneUIRegistrar)}: 対象が Presenter ではありません。PresenterBase<,> 派生を配置してください。 node={node.GetType().Name}");
                return;
            }

            UniqueId = UIManager.Instance.RegisterSceneNode(node, closeOnRegister: _closeOnRegister);
        }
    }
}