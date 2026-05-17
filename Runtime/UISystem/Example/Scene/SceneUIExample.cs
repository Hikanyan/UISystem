using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HikanyanLibrary.UISystem.Example
{
    public sealed class SceneUIExample : MonoBehaviour
    {
        // デモ用：OでOpen / CでClose
        [SerializeField] private Key _openKey = Key.O;
        [SerializeField] private Key _closeKey = Key.C;

        private void Awake()
        {
             _ = UIManager.Instance.OpenSceneAsync<SceneUIExamplePresenter, SceneUIExampleParam>(
                 new SceneUIExampleParam("Awake message"),
                 this.GetCancellationTokenOnDestroy());
        }

        private void Update()
        {
            // Keyboard が無い環境（モバイル等）では null になり得る
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb[_openKey].wasPressedThisFrame)
            {
                Open("Hello Scene UI!").Forget();
            }

            if (kb[_closeKey].wasPressedThisFrame)
            {
                Close().Forget();
            }
        }

        private async UniTask Open(string message, CancellationToken ct = default)
        {
            await UIManager.Instance.OpenSceneAsync<SceneUIExamplePresenter, SceneUIExampleParam>(
                new SceneUIExampleParam(message),
                ct);
        }

        private async UniTask Close(CancellationToken ct = default)
        {
            await UIManager.Instance.CloseSceneAsync<SceneUIExamplePresenter>(ct);
        }
    }
}