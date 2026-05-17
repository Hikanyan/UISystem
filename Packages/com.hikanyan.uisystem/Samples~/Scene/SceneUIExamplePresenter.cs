using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.UISystem.Example
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SceneUIRegistrar))]
    [RequireComponent(typeof(SceneUIExampleView))]
    public sealed class SceneUIExamplePresenter
        : PresenterBase<SceneUIExampleView, SceneUIExampleParam>
    {
        protected override void OnBind()
        {
            // Model -> View 反映
            View.SetMessage(Model.Message);
        }

        protected override UniTask OnOpenInternalAsync(CancellationToken cancellationToken)
        {
            // ここでフェードイン等（必要なら）
            return UniTask.CompletedTask;
        }

        protected override UniTask OnCloseInternalAsync(CancellationToken cancellationToken)
        {
            // ここでフェードアウト等（必要なら）
            return UniTask.CompletedTask;
        }
    }
}