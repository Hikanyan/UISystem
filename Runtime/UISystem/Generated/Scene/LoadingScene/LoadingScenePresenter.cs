using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SceneUIRegistrar))]
    [RequireComponent(typeof(LoadingSceneView))]
    public class LoadingScenePresenter : PresenterBase<LoadingSceneView, LoadingSceneParam>
    {
        private IProgress<float> _internalProgress;

        protected override void OnBind()
        {
            View.SetMessage(Model.Message);
            View.SetProgress(0f);
            View.SetSpinnerActive(true);

            // Presenter 側で Progress をラップし、View 更新を統一する
            _internalProgress = new Progress<float>(p =>
            {
                View.SetProgress(p);
                // 外部にも流したいなら Model.Progress に中継
                Model.Progress?.Report(p);
            });
        }

        protected override async UniTask OnOpenInternalAsync(CancellationToken cancellationToken)
        {
            // ロード処理を実行
            try
            {
                // LoadAsync 内で _internalProgress.Report(x) を呼びたい場合は、
                // LoadAsync 実装側が IProgress<float> を捕捉できる形にする必要があります。
                // ここでは「LoadAsync だけ実行」し、進捗は任意設計にしています。
                await Model.LoadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // キャンセルは通常動作として扱う（必要ならメッセージ差し替え）
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                // ここでエラー表示UIに切り替える等も可能
                throw;
            }
            finally
            {
                View.SetSpinnerActive(false);
            }

            // 完了したら自分を閉じる（SceneResident でも Close は可能）
            // ※ Close の実装は UINodeBase 側に依存
            await UIManager.Instance.CloseSceneAsync<LoadingScenePresenter>(cancellationToken);
        }

        protected override UniTask OnCloseInternalAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }
    }
}
