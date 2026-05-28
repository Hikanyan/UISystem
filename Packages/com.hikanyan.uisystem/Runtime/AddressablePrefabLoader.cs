using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// using VContainer;
// using VContainer.Unity;

namespace HikanyanLibrary.UISystem
{
    public static class AddressablePrefabLoader
    {
        // public static async UniTask<T> LoadAndInstantiateAsync<T, TSceneScope>(string prefabKey,
        //     CancellationToken cancellationToken = default)
        //     where T : MonoBehaviour
        //     where TSceneScope : LifetimeScope
        // {
        //     var handle = Addressables.LoadAssetAsync<GameObject>(prefabKey);
        //     await handle.ToUniTask(cancellationToken: cancellationToken);
        //
        //     var prefab = handle.Result;
        //     var scope = LifetimeScope.Find<TSceneScope>();
        //     var instance = scope.Container.Instantiate(prefab);
        //     var component = instance.GetComponent<T>();
        //     scope.Container.Inject(component);
        //     return component;
        // }
        //
        // public static async UniTask<T> LoadAndInstantiateAsync<T, TSceneScope>(string prefabKey, Transform parent,
        //     CancellationToken cancellationToken = default)
        //     where T : MonoBehaviour
        //     where TSceneScope : LifetimeScope
        // {
        //     var handle = Addressables.LoadAssetAsync<GameObject>(prefabKey);
        //     await handle.ToUniTask(cancellationToken: cancellationToken);
        //
        //     var prefab = handle.Result;
        //     var scope = LifetimeScope.Find<TSceneScope>();
        //     var instance = scope.Container.Instantiate(prefab, parent);
        //     var component = instance.GetComponent<T>();
        //     scope.Container.Inject(component);
        //     return component;
        // }

        public static async UniTask<T> LoadAndInstantiateAsync<T>(string prefabKey,
            CancellationToken cancellationToken = default)
            where T : MonoBehaviour
        {
            AsyncOperationHandle<GameObject> handle = default;
            try
            {
                handle = Addressables.LoadAssetAsync<GameObject>(prefabKey);
                await handle.ToUniTask(cancellationToken: cancellationToken);

                var prefab = handle.Result;

                var instance = Object.Instantiate(prefab);
                var component = instance.GetComponent<T>();
                return component;
            }
            catch (System.Exception)
            {
                Debug.LogError($"AddressablePrefabLoader: prefabKey={prefabKey} から {typeof(T).Name} を取得できませんでした。");
                return null;
            }
            // finally
            // {
            //     if (handle.IsValid())
            //     {
            //         Addressables.Release(handle);
            //     }
            // }
        }

        public static async UniTask<T> LoadAndInstantiateAsync<T>(string prefabKey, Transform parent,
            CancellationToken cancellationToken = default)
            where T : MonoBehaviour
        {
            AsyncOperationHandle<GameObject> handle = default;
            try
            {
                handle = Addressables.LoadAssetAsync<GameObject>(prefabKey);
                await handle.ToUniTask(cancellationToken: cancellationToken);

                var prefab = handle.Result;

                var instance = Object.Instantiate(prefab, parent);
                var component = instance.GetComponent<T>();
                return component;
            }
            catch (System.Exception)
            {
                Debug.LogError($"AddressablePrefabLoader: prefabKey={prefabKey} から {typeof(T).Name} を取得できませんでした。");
                return null;
            }
            // finally
            // {
            //     if (handle.IsValid())
            //     {
            //         Addressables.Release(handle);
            //     }
            // }
        }
    }
}
