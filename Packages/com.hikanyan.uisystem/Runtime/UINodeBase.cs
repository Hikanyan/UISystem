using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    /// <summary>
    /// UI に渡すデータ（モデル）基底
    /// - 必要に応じて派生クラスでパラメータ定義
    /// </summary>
    public abstract class Parameter
    {
    }
    public abstract class UINodeBase : MonoBehaviour, IUINode
    {
        public int UniqueId { get; private set; } = -1;
        public bool IsOpen { get; private set; }

        protected UIManager Owner { get; private set; }

        
        /// <summary>
        /// UIManager からのみ呼ばれる内部初期化
        /// </summary>
        internal void InternalSetup(int uniqueId, UIManager owner)
        {
            UniqueId = uniqueId;
            Owner = owner;
        }
        
        /// <summary>
        /// Scene 常駐UIの初期状態（active と IsOpen）を揃えるために使用
        /// UIManager / SceneUIRegistrar からのみ呼ぶ
        /// </summary>
        internal void InternalSetInitialState(bool isOpen, bool active)
        {
            IsOpen = isOpen;
            gameObject.SetActive(active);
        }
        
        public async UniTask OpenAsync(Parameter parameter, CancellationToken cancellationToken)
        {
            if (IsOpen) return;

            // 開く前に有効化（必要なら派生で上書きしてもよい）
            gameObject.SetActive(true);

            await OnOpenAsync(parameter, cancellationToken);
            IsOpen = true;
        }

        public async UniTask CloseAsync(CancellationToken cancellationToken)
        {
            if (!IsOpen) return;

            await OnCloseAsync(cancellationToken);
            IsOpen = false;

            // Close 完了後に無効化（Destroy は UIManager が行う）
            gameObject.SetActive(false);
        }

        /// <summary>開く処理（アニメ/バインド等）</summary>
        protected abstract UniTask OnOpenAsync(Parameter parameter, CancellationToken cancellationToken);

        /// <summary>閉じる処理（アニメ等）</summary>
        protected abstract UniTask OnCloseAsync(CancellationToken cancellationToken);
    }
}