using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.UISystem.Example
{
    public sealed class InstanceExample : MonoBehaviour
    {
        private UIManager _ui => UIManager.Instance;

        public void Start()
        {
            Test(this.GetCancellationTokenOnDestroy()).Forget();
        }

        public async UniTask Test(CancellationToken ct)
        {
            var id = await _ui.OpenAsync<PopupMessagePresenter, PopupMessageParam>(
                prefabKey: "PopupMessagePresenter",
                parameter: new PopupMessageParam { Title = "Title", Body = "Hello" },
                cancellationToken: ct);

            // ...任意タイミングで
            await _ui.CloseAsync(id, ct);
        }
    }
}