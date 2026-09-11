using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.Tool
{
    /// <summary>Compatibility facade. Resource ownership is implemented by UISystem.</summary>
    [System.Obsolete("Use HikanyanLibrary.UISystem.AddressablePrefabLoader.")]
    public static class AddressablePrefabLoader
    {
        public static UniTask<T> LoadAndInstantiateAsync<T>(string key, CancellationToken cancellationToken = default) where T : MonoBehaviour =>
            UISystem.AddressablePrefabLoader.LoadAndInstantiateAsync<T>(key, cancellationToken);
        public static UniTask<T> LoadAndInstantiateAsync<T>(string key, Transform parent, CancellationToken cancellationToken = default) where T : MonoBehaviour =>
            UISystem.AddressablePrefabLoader.LoadAndInstantiateAsync<T>(key, parent, cancellationToken);
    }
}
