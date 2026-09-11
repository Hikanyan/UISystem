using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HikanyanLibrary.UISystem
{
    public sealed class UIBackHandler : MonoBehaviour, ICancelHandler
    {
        [SerializeField] private UIManager _manager;
        public void Initialize(UIManager manager) => _manager = manager;
        public void OnCancel(BaseEventData eventData)
        {
            if (_manager == null) _manager = GetComponentInParent<UIManager>();
            if (_manager != null) _manager.BackAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }
    }
}
