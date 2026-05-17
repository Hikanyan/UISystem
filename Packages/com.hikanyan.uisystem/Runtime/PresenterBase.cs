using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    public abstract class PresenterBase<TView, TModel> : UINodeBase, IPresenterNode
        where TView : Component
        where TModel : Parameter
    {
        protected TView View { get; private set; }
        protected TModel Model { get; private set; }

        protected virtual void Awake()
        {
            View = GetComponent<TView>();
            if (View != null) return;
            // Presenter が付いている GameObject 直下/子から View を探す
            View = GetComponentInChildren<TView>(true);
            if (View == null)
            {
                Debug.LogError($"{GetType().Name}: View<{typeof(TView).Name}> が見つかりません。");
            }
        }

        protected sealed override async UniTask OnOpenAsync(Parameter parameter, CancellationToken cancellationToken)
        {
            if (parameter is not TModel model)
            {
                throw new System.ArgumentException(
                    $"{GetType().Name}: parameter の型が不正です。期待={typeof(TModel).Name}, 実際={parameter?.GetType().Name ?? "null"}");
            }

            Model = model;

            // バインド
            OnBind();

            // 派生の開く処理（アニメ等）
            await OnOpenInternalAsync(cancellationToken);
        }

        protected sealed override async UniTask OnCloseAsync(CancellationToken cancellationToken)
        {
            await OnCloseInternalAsync(cancellationToken);
            OnUnbind();
        }

        /// <summary>Model → View 反映など</summary>
        protected virtual void OnBind() { }

        /// <summary>購読解除など（R3/UniRx を使うならここで Dispose）</summary>
        protected virtual void OnUnbind() { }

        /// <summary>派生で開くアニメ等を書く</summary>
        protected virtual UniTask OnOpenInternalAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;

        /// <summary>派生で閉じるアニメ等を書く</summary>
        protected virtual UniTask OnCloseInternalAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;
    }
}