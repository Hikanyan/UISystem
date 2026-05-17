using System.Threading;
using Cysharp.Threading.Tasks;

namespace HikanyanLibrary.UISystem
{
    public interface IUINode
    {
        int UniqueId { get; }
        bool IsOpen { get; }

        /// <summary>UI を開く（表示開始）</summary>
        UniTask OpenAsync(Parameter parameter, CancellationToken cancellationToken);

        /// <summary>UI を閉じる（非表示完了まで）</summary>
        UniTask CloseAsync(CancellationToken cancellationToken);
    }
}