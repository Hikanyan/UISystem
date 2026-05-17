using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SceneUIRegistrar))]
    [RequireComponent(typeof(NewUIView))]
    public class NewUIPresenter : PresenterBase<NewUIView, NewUIParam>
    {
        protected override void OnBind()
        {
            View.SetMessage(Model.Message);
        }

        protected override UniTask OnOpenInternalAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        protected override UniTask OnCloseInternalAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }
    }
}
