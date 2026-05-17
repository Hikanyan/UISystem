using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HikanyanLibrary.UISystem
{
    public sealed class LoadingSceneParam : Parameter
    {
        public readonly string Message;
        public readonly Func<CancellationToken, UniTask> LoadAsync;
        public readonly IProgress<float> Progress;

        // ★追加：表示だけしたい用途向け
        public LoadingSceneParam(string message)
            : this(message, _ => UniTask.CompletedTask, null)
        {
        }

        public LoadingSceneParam(
            string message,
            Func<CancellationToken, UniTask> loadAsync,
            IProgress<float> progress = null)
        {
            Message = message;
            LoadAsync = loadAsync ?? throw new ArgumentNullException(nameof(loadAsync));
            Progress = progress;
        }
    }
}
