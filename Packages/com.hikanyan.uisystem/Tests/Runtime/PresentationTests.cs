using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HikanyanLibrary.UISystem.Tests
{
    public sealed class TestDialog : DialogPresenterBase<TestView, TestArgs, bool>
    {
        protected override void OnDialogBind() { }
        public void Confirm() => CompleteResult(true);
    }
    public sealed class PresentationTests
    {
        private GameObject _root;
        private UIManager _manager;
        private UICatalog _catalog;
        private CountingLoader _loader;
        private sealed class CountingLoader : IUIViewLoader
        {
            public int Loads, Releases;
            public bool FailBind;
            public UINodeBase Last;
            public UniTask<UINodeBase> LoadAsync(UIDefinition definition, Transform parent, CancellationToken token)
            {
                Loads++;
                var go = new GameObject("loaded", typeof(RectTransform), typeof(TestView));
                go.transform.SetParent(parent, false);
                if (definition.Id == "dialog") Last = go.AddComponent<TestDialog>();
                else
                {
                    var node = go.AddComponent<TestPresenter>();
                    node.FailBind = FailBind;
                    Last = node;
                }
                return UniTask.FromResult(Last);
            }
            public void Release(UINodeBase node)
            {
                if (node == null) return;
                Releases++;
                node.Dispose();
                UnityEngine.Object.Destroy(node.gameObject);
            }
        }
        [SetUp] public void Setup()
        {
            _root = new GameObject("manager");
            _manager = _root.AddComponent<UIManager>();
            _catalog = ScriptableObject.CreateInstance<UICatalog>();
            _manager.Catalog = _catalog;
            _loader = new CountingLoader();
            _manager.Loader = _loader;
        }
        [TearDown] public void Teardown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            foreach (var entry in _catalog.Screens) UnityEngine.Object.DestroyImmediate(entry);
            UnityEngine.Object.DestroyImmediate(_catalog);
        }
        private UIDefinition Add(string id, UILayer layer = UILayer.Page, UIReuse reuse = UIReuse.Destroy)
        {
            var definition = ScriptableObject.CreateInstance<UIDefinition>();
            definition.Id = id; definition.Layer = layer; definition.Reuse = reuse;
            _catalog.Screens.Add(definition);
            return definition;
        }
        private static UIKey<TestArgs> Key(string id) => UIKey<TestArgs>.For<TestPresenter>(id);

        [UnityTest] public IEnumerator DuplicateOpenSharesPresentation() => UniTask.ToCoroutine(async () =>
        {
            Add("page");
            var first = await _manager.OpenAsync(Key("page"), new TestArgs());
            var second = await _manager.OpenAsync(Key("page"), new TestArgs());
            Assert.AreSame(first, second);
            Assert.AreEqual(1, _loader.Loads);
            await first.CloseAsync();
            await second.CloseAsync();
            Assert.AreEqual(1, _loader.Releases);
            Assert.AreEqual(0, _manager.RegisteredCount);
        });
        [UnityTest] public IEnumerator CachedReopenDoesNotLetOldHandleCloseNewPresentation() => UniTask.ToCoroutine(async () =>
        {
            Add("page", reuse: UIReuse.KeepAlive);
            var first = await _manager.OpenAsync(Key("page"), new TestArgs());
            await first.CloseAsync();
            Assert.AreEqual(1, _manager.CachedCount);
            var second = await _manager.OpenAsync(Key("page"), new TestArgs());
            await first.CloseAsync();
            Assert.IsTrue(second.Node.IsOpen);
            Assert.AreEqual(1, _loader.Loads);
            Assert.AreEqual(2, ((TestPresenter)second.Node).BindCount);
            await second.CloseAsync();
            _manager.ClearCache();
            Assert.AreEqual(1, _loader.Releases);
        });
        [UnityTest] public IEnumerator FailedOpenReleasesCustomLoaderInstance() => UniTask.ToCoroutine(async () =>
        {
            Add("page"); _loader.FailBind = true;
            try { await _manager.OpenAsync(Key("page"), new TestArgs()); Assert.Fail("Expected failure"); }
            catch (InvalidOperationException) { }
            Assert.AreEqual(1, _loader.Releases);
            Assert.AreEqual(0, _manager.RegisteredCount);
        });
        [UnityTest] public IEnumerator ModalBlocksPageAndBackRestoresIt() => UniTask.ToCoroutine(async () =>
        {
            Add("page"); Add("modal", UILayer.Modal);
            var page = await _manager.OpenAsync(Key("page"), new TestArgs());
            var modal = await _manager.OpenAsync(Key("modal"), new TestArgs());
            Assert.IsFalse(page.Node.GetComponentInParent<CanvasGroup>().interactable);
            Assert.IsTrue(modal.Node.GetComponentInParent<CanvasGroup>().interactable);
            Assert.IsTrue(await _manager.BackAsync());
            Assert.IsTrue(modal.IsClosed);
            Assert.IsTrue(page.Node.GetComponentInParent<CanvasGroup>().interactable);
        });
        [UnityTest] public IEnumerator DialogReturnsResultAndReleases() => UniTask.ToCoroutine(async () =>
        {
            Add("dialog", UILayer.Modal);
            var result = _manager.ShowDialogAsync(UIDialogKey<TestArgs, bool>.For<TestDialog>("dialog"), new TestArgs());
            ((TestDialog)_loader.Last).Confirm();
            Assert.IsTrue(await result);
            Assert.AreEqual(1, _loader.Releases);
        });
        [UnityTest] public IEnumerator BackCancelsDialogResult() => UniTask.ToCoroutine(async () =>
        {
            Add("dialog", UILayer.Modal);
            var result = _manager.ShowDialogAsync(UIDialogKey<TestArgs, bool>.For<TestDialog>("dialog"), new TestArgs()).SuppressCancellationThrow();
            await _manager.BackAsync();
            Assert.IsTrue((await result).IsCanceled);
            Assert.AreEqual(1, _loader.Releases);
        });
        [Test] public void CatalogReportsDuplicateIds()
        {
            Add("duplicate"); Add("duplicate");
            Assert.Throws<InvalidOperationException>(() => _catalog.Get("duplicate"));
        }
    }
}
