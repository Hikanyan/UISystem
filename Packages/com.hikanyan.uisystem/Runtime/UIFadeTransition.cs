using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIFadeTransition : MonoBehaviour
    {
        [Min(0)] public float Duration = 0.15f;
        public async UniTask PlayAsync(bool opening, CancellationToken token)
        {
            var group = GetComponent<CanvasGroup>();
            var from = opening ? 0f : group.alpha;
            var to = opening ? 1f : 0f;
            group.alpha = from;
            for (var elapsed = 0f; elapsed < Duration; elapsed += Time.unscaledDeltaTime)
            {
                token.ThrowIfCancellationRequested();
                group.alpha = Mathf.Lerp(from, to, elapsed / Duration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            group.alpha = to;
        }
    }
}
