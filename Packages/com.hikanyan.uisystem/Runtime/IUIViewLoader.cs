using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    /// <summary>Return one owned instance. Release must also support inactive or partially opened nodes.</summary>
    public interface IUIViewLoader
    {
        UniTask<UINodeBase> LoadAsync(UIDefinition definition, Transform parent, CancellationToken token);
        void Release(UINodeBase node);
    }
    public sealed class DefaultUIViewLoader : IUIViewLoader
    {
        public UniTask<UINodeBase> LoadAsync(UIDefinition definition, Transform parent, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (definition.Prefab != null)
                return UniTask.FromResult(UnityEngine.Object.Instantiate(definition.Prefab, parent, false));
            return AddressablePrefabLoader.LoadAndInstantiateAsync<UINodeBase>(definition.AddressableKey, parent, token);
        }
        public void Release(UINodeBase node)
        {
            if (node == null) return;
            node.Dispose();
            var ownership = node.GetComponent<AddressableInstanceOwner>();
            if (ownership != null) ownership.Release();
            UnityEngine.Object.Destroy(node.gameObject);
        }
    }
}
