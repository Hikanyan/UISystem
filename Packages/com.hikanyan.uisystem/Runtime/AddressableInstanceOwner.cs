using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HikanyanLibrary.UISystem
{
    [AddComponentMenu("")]
    public sealed class AddressableInstanceOwner : MonoBehaviour
    {
        private AsyncOperationHandle<GameObject> _handle;
        internal void Initialize(AsyncOperationHandle<GameObject> handle) => _handle = handle;
        private void OnDestroy() => Release();
        internal void Release()
        {
            if (_handle.IsValid()) Addressables.Release(_handle);
            _handle = default;
        }
    }
}
