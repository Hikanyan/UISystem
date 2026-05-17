using Cysharp.Threading.Tasks;

namespace HikanyanLibrary.UISystem.Example
{
    public sealed class PopupMessageParam : Parameter
    {
        public string Title;
        public string Body;
    }


    public sealed class PopupMessagePresenter : PresenterBase<PopupMessageView, PopupMessageParam>
    {
        protected override void OnBind()
        {
            // Model の内容を View に反映
            View.TitleText = Model.Title;
            View.BodyText = Model.Body;
        }

        protected override UniTask OnOpenInternalAsync(System.Threading.CancellationToken cancellationToken)
        {
            // フェードイン等
            return UniTask.CompletedTask;
        }

        protected override UniTask OnCloseInternalAsync(System.Threading.CancellationToken cancellationToken)
        {
            // フェードアウト等
            return UniTask.CompletedTask;
        }
    }
}