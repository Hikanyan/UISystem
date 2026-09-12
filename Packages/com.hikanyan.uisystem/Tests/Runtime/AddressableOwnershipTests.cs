using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;

namespace HikanyanLibrary.UISystem.Tests
{
    public sealed class AddressablesTestSetup : IPrebuildSetup
    {
        public void Setup()
        {
#if UNITY_EDITOR
            var settings = Type.GetType("UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");
            settings?.GetMethod("GetSettings", new[] { typeof(bool) })?.Invoke(null, new object[] { true });
#endif
        }
    }
    [PrebuildSetup(typeof(AddressablesTestSetup))]
    public sealed class AddressableOwnershipTests
    {
        private sealed class TestProvider : ResourceProviderBase
        {
            public override string ProviderId { get; } = Guid.NewGuid().ToString();
            public GameObject Prefab;
            public int Releases;
            public bool Delay;
            public bool Started;
            public ProvideHandle Pending;
            public override Type GetDefaultType(IResourceLocation location) => typeof(GameObject);
            public override void Provide(ProvideHandle handle)
            {
                Started = true;
                if (Delay) Pending = handle;
                else handle.Complete(Prefab, true, null);
            }
            public override void Release(IResourceLocation location, object asset) => Releases++;
        }

        [UnityTest] public IEnumerator RepeatedLoadDestroyReleasesEveryReference() => UniTask.ToCoroutine(async () =>
        {
            await Addressables.InitializeAsync().ToUniTask();
            var prefab = new GameObject("prototype", typeof(TestView), typeof(TestPresenter));
            var provider = new TestProvider { Prefab = prefab };
            var locator = Register(provider);
            try
            {
                for (var i = 0; i < 20; i++)
                {
                    var node = await AddressablePrefabLoader.LoadAndInstantiateAsync<TestPresenter>("uisystem-ownership-test");
                    Assert.IsNotNull(node);
                    UnityEngine.Object.Destroy(node.gameObject);
                    using var releaseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await UniTask.WaitUntil(() => provider.Releases == i + 1, cancellationToken: releaseTimeout.Token);
                }
                Assert.AreEqual(20, provider.Releases);
            }
            finally { Unregister(locator, provider); UnityEngine.Object.DestroyImmediate(prefab); }
        });
        [UnityTest] public IEnumerator MissingComponentReleasesLoad() => UniTask.ToCoroutine(async () =>
        {
            await Addressables.InitializeAsync().ToUniTask();
            var prefab = new GameObject("invalid prototype");
            var provider = new TestProvider { Prefab = prefab };
            var locator = Register(provider);
            try
            {
                try { await AddressablePrefabLoader.LoadAndInstantiateAsync<TestPresenter>("uisystem-ownership-test"); Assert.Fail("Expected missing component"); }
                catch (InvalidOperationException) { }
                using var releaseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await UniTask.WaitUntil(() => provider.Releases == 1, cancellationToken: releaseTimeout.Token);
                Assert.AreEqual(1, provider.Releases);
            }
            finally { Unregister(locator, provider); UnityEngine.Object.DestroyImmediate(prefab); }
        });
        [UnityTest] public IEnumerator CancelledLoadReleasesWhenProviderCompletes() => UniTask.ToCoroutine(async () =>
        {
            await Addressables.InitializeAsync().ToUniTask();
            var prefab = new GameObject("prototype", typeof(TestView), typeof(TestPresenter));
            var provider = new TestProvider { Prefab = prefab, Delay = true };
            var locator = Register(provider);
            try
            {
                using var cts = new CancellationTokenSource();
                var loading = AddressablePrefabLoader.LoadAndInstantiateAsync<TestPresenter>("uisystem-ownership-test", cts.Token).SuppressCancellationThrow();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await UniTask.WaitUntil(() => provider.Started, cancellationToken: timeout.Token);
                cts.Cancel();
                Assert.IsTrue((await loading).IsCanceled);
                provider.Pending.Complete(prefab, true, null);
                await UniTask.NextFrame();
                Assert.AreEqual(1, provider.Releases);
            }
            finally { Unregister(locator, provider); UnityEngine.Object.DestroyImmediate(prefab); }
        });
        private static ResourceLocationMap Register(TestProvider provider)
        {
            Addressables.ResourceManager.ResourceProviders.Add(provider);
            var locator = new ResourceLocationMap("UISystem ownership test");
            locator.Add("uisystem-ownership-test", new ResourceLocationBase("uisystem-ownership-test", "test", provider.ProviderId, typeof(GameObject)));
            Addressables.AddResourceLocator(locator);
            return locator;
        }
        private static void Unregister(ResourceLocationMap locator, TestProvider provider)
        {
            Addressables.RemoveResourceLocator(locator);
            Addressables.ResourceManager.ResourceProviders.Remove(provider);
        }
    }
}
