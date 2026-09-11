using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace HikanyanLibrary.UISystem
{
    public static class AddressablePrefabLoader
    {
        public static UniTask<T> LoadAndInstantiateAsync<T>(string prefabKey, CancellationToken cancellationToken = default)
            where T : MonoBehaviour => LoadAndInstantiateAsync<T>(prefabKey, null, cancellationToken);

        public static async UniTask<T> LoadAndInstantiateAsync<T>(string prefabKey, Transform parent,
            CancellationToken cancellationToken = default) where T : MonoBehaviour
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(prefabKey)) throw new ArgumentException("A prefab key is required.", nameof(prefabKey));
            var handle = Addressables.LoadAssetAsync<GameObject>(prefabKey);
            GameObject instance = null;
            var transferred = false;
            try
            {
                await handle.ToUniTask(cancellationToken: cancellationToken);
                var prefab = handle.Result;
                cancellationToken.ThrowIfCancellationRequested();
                if (prefab.GetComponent<T>() == null)
                    throw new InvalidOperationException($"Prefab '{prefabKey}' must have {typeof(T).Name} on its root.");
                instance = UnityEngine.Object.Instantiate(prefab, parent, false);
                instance.AddComponent<AddressableInstanceOwner>().Initialize(handle);
                transferred = true;
                return instance.GetComponent<T>();
            }
            catch
            {
                if (instance != null) UnityEngine.Object.Destroy(instance);
                throw;
            }
            finally
            {
                if (!transferred && handle.IsValid()) Addressables.Release(handle);
            }
        }
    }
}
