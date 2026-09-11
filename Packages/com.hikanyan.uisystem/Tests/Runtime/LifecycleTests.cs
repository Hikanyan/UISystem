using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HikanyanLibrary.UISystem.Tests
{
    public sealed class TestArgs : Parameter { }
    public sealed class TestView : MonoBehaviour { }
    public sealed class TestPresenter : PresenterBase<TestView, TestArgs>
    {
        public int BindCount;
        public int UnbindCount;
        public bool FailBind;
        public bool SlowOpen;
        public bool SlowClose;
        protected override void OnBind()
        {
            BindCount++;
            if (FailBind) throw new InvalidOperationException("bind failed");
        }
        protected override void OnUnbind() => UnbindCount++;
        protected override UniTask OnOpenInternalAsync(CancellationToken token) =>
            SlowOpen ? UniTask.Delay(10000, cancellationToken: token) : UniTask.CompletedTask;
        protected override UniTask OnCloseInternalAsync(CancellationToken token) =>
            SlowClose ? UniTask.Delay(10000, cancellationToken: token) : UniTask.CompletedTask;
    }

    public sealed class LifecycleTests
    {
        private GameObject _root;
        private TestPresenter _node;
        [SetUp] public void Setup()
        {
            _root = new GameObject("test ui", typeof(TestView));
            _node = _root.AddComponent<TestPresenter>();
        }
        [TearDown] public void Teardown()
        {
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }

        [UnityTest] public IEnumerator RepeatedOpenCloseBindsOncePerPresentation() => UniTask.ToCoroutine(async () =>
        {
            for (var i = 0; i < 100; i++)
            {
                await _node.OpenAsync(new TestArgs());
                await _node.OpenAsync(new TestArgs());
                await _node.CloseAsync();
                await _node.CloseAsync();
            }
            Assert.AreEqual(100, _node.BindCount);
            Assert.AreEqual(100, _node.UnbindCount);
            Assert.AreEqual(UIState.Closed, _node.State);
            Assert.IsFalse(_root.activeSelf);
        });

        [UnityTest] public IEnumerator CloseInterruptsOpeningAndUnbindsOnce() => UniTask.ToCoroutine(async () =>
        {
            _node.SlowOpen = true;
            var opening = _node.OpenAsync(new TestArgs()).SuppressCancellationThrow();
            await _node.CloseAsync();
            Assert.IsTrue(await opening);
            Assert.AreEqual(UIState.Closed, _node.State);
            Assert.AreEqual(1, _node.UnbindCount);
        });

        [UnityTest] public IEnumerator FailedBindRollsBack() => UniTask.ToCoroutine(async () =>
        {
            _node.FailBind = true;
            try { await _node.OpenAsync(new TestArgs()); Assert.Fail("Expected bind failure"); }
            catch (InvalidOperationException) { }
            Assert.AreEqual(UIState.Closed, _node.State);
            Assert.AreEqual(1, _node.UnbindCount);
            Assert.IsFalse(_root.activeSelf);
        });

        [UnityTest] public IEnumerator CancelledCloseStillCleansUp() => UniTask.ToCoroutine(async () =>
        {
            await _node.OpenAsync(new TestArgs());
            _node.SlowClose = true;
            using var cts = new CancellationTokenSource();
            var closing = _node.CloseAsync(cts.Token).SuppressCancellationThrow();
            cts.Cancel();
            Assert.IsTrue(await closing);
            Assert.AreEqual(UIState.Closed, _node.State);
            Assert.AreEqual(1, _node.UnbindCount);
        });

        [UnityTest] public IEnumerator DestroyInterruptsOpening() => UniTask.ToCoroutine(async () =>
        {
            _node.SlowOpen = true;
            var opening = _node.OpenAsync(new TestArgs()).SuppressCancellationThrow();
            _node.Dispose();
            Assert.IsTrue(await opening);
            Assert.AreEqual(UIState.Disposed, _node.State);
            Assert.AreEqual(1, _node.UnbindCount);
        });

        [UnityTest] public IEnumerator SceneLifetimeRejectsDuplicateAndUnregisters() => UniTask.ToCoroutine(async () =>
        {
            var managerObject = new GameObject("manager");
            var other = new GameObject("other", typeof(TestView));
            try
            {
                var manager = managerObject.AddComponent<UIManager>();
                manager.RegisterSceneNode(_node);
                Assert.Throws<InvalidOperationException>(() => manager.RegisterSceneNode(other.AddComponent<TestPresenter>()));
                Assert.AreEqual(1, manager.RegisteredCount);
                await manager.OpenSceneAsync<TestPresenter, TestArgs>(new TestArgs());
                _node.Dispose();
                Assert.AreEqual(0, manager.RegisteredCount);
                Assert.AreEqual(1, _node.UnbindCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(other);
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        });

        [UnityTest] public IEnumerator InitiallyVisibleSceneCanClose() => UniTask.ToCoroutine(async () =>
        {
            var go = new GameObject("manager");
            try
            {
                var manager = go.AddComponent<UIManager>();
                var id = manager.RegisterSceneNode(_node, false);
                Assert.IsTrue(_node.IsOpen);
                await manager.CloseAsync(id);
                Assert.IsFalse(_root.activeSelf);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        });
    }
}
