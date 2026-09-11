using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    public interface IUIResult<TResult> { UniTask<TResult> WaitForResultAsync(CancellationToken token); }
    public abstract class DialogPresenterBase<TView, TArgs, TResult> : PresenterBase<TView, TArgs>, IUIResult<TResult>
        where TView : Component where TArgs : Parameter
    {
        private UniTaskCompletionSource<TResult> _result;
        protected sealed override void OnBind()
        {
            _result = new UniTaskCompletionSource<TResult>();
            OnDialogBind();
        }
        protected sealed override void OnUnbind()
        {
            _result?.TrySetCanceled();
            OnDialogUnbind();
        }
        protected bool CompleteResult(TResult value) => _result != null && _result.TrySetResult(value);
        public UniTask<TResult> WaitForResultAsync(CancellationToken token) => _result.Task.AttachExternalCancellation(token);
        protected abstract void OnDialogBind();
        protected virtual void OnDialogUnbind() { }
    }
}
