using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace HikanyanLibrary.Tool
{
    public static class AddressablePrefabLoader
    {
        public static async UniTask<T> LoadAndInstantiateAsync<T>(string prefabKey, CancellationToken cancellationToken = default)
            where T : MonoBehaviour
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(prefabKey);
            await handle.ToUniTask(cancellationToken: cancellationToken);

            var prefab = handle.Result;
            var instance = Object.Instantiate(prefab);
            return instance.GetComponent<T>();
        }

        public static async UniTask<T> LoadAndInstantiateAsync<T>(string prefabKey, Transform parent, CancellationToken cancellationToken = default)
            where T : MonoBehaviour
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(prefabKey);
            await handle.ToUniTask(cancellationToken: cancellationToken);

            var prefab = handle.Result;
            var instance = Object.Instantiate(prefab, parent);
            return instance.GetComponent<T>();
        }
    }
}