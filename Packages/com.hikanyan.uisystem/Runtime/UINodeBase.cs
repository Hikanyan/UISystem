using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    /// <summary>Arguments for one presentation; game state belongs to game services.</summary>
    public abstract class Parameter { }
    public enum UIState { Closed, Opening, Open, Closing, Disposed }

    /// <summary>Main-thread lifecycle. Close interrupts Opening; other operations are serialized.</summary>
    public abstract class UINodeBase : MonoBehaviour, IUINode
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly CancellationTokenSource _lifetime = new();
        private CancellationTokenSource _opening;
        public int UniqueId { get; private set; } = -1;
        public UIState State { get; private set; } = UIState.Closed;
        public bool IsOpen => State == UIState.Open;
        protected UIManager Owner { get; private set; }
        public event Action<UINodeBase> Disposed;

        internal void InternalSetup(int id, UIManager owner)
        {
            if (Owner != null && Owner != owner)
                throw new InvalidOperationException("A UI node cannot belong to two managers.");
            UniqueId = id;
            Owner = owner;
        }

        internal void InternalSetInitialState(bool isOpen, bool active)
        {
            if (State != UIState.Closed) return;
            State = isOpen ? UIState.Open : UIState.Closed;
            gameObject.SetActive(active);
        }

        public async UniTask OpenAsync(Parameter parameter, CancellationToken cancellationToken = default)
        {
            if (State == UIState.Disposed) throw new ObjectDisposedException(GetType().Name);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
            await _gate.WaitAsync(linked.Token);
            try
            {
                linked.Token.ThrowIfCancellationRequested();
                if (State == UIState.Open) return;
                _opening = linked;
                State = UIState.Opening;
                gameObject.SetActive(true);
                try
                {
                    await OnOpenAsync(parameter, linked.Token);
                    linked.Token.ThrowIfCancellationRequested();
                    State = UIState.Open;
                }
                catch
                {
                    CleanupSafely();
                    if (State != UIState.Disposed)
                    {
                        State = UIState.Closed;
                        if (this != null) gameObject.SetActive(false);
                    }
                    throw;
                }
            }
            finally { _opening = null; _gate.Release(); }
        }

        public async UniTask CloseAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (State == UIState.Disposed) return;
            _opening?.Cancel();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
            await _gate.WaitAsync(linked.Token);
            try
            {
                if (State == UIState.Closed || State == UIState.Disposed) return;
                State = UIState.Closing;
                try { await OnCloseAsync(linked.Token); }
                finally
                {
                    CleanupSafely();
                    if (State != UIState.Disposed)
                    {
                        State = UIState.Closed;
                        if (this != null) gameObject.SetActive(false);
                    }
                }
            }
            finally { _gate.Release(); }
        }

        /// <summary>Synchronous cleanup also used when the owning scope ends.</summary>
        public void Dispose()
        {
            if (State == UIState.Disposed) return;
            State = UIState.Disposed;
            try { _lifetime.Cancel(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
            CleanupSafely();
            if (this != null) gameObject.SetActive(false);
            var callbacks = Disposed;
            Disposed = null;
            if (callbacks != null)
                foreach (Action<UINodeBase> callback in callbacks.GetInvocationList())
                    try { callback(this); }
                    catch (Exception exception) { Debug.LogException(exception, this); }
        }

        protected virtual void OnDestroy() => Dispose();
        protected virtual void OnCleanup() { }
        private void CleanupSafely()
        {
            try { OnCleanup(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
        protected abstract UniTask OnOpenAsync(Parameter parameter, CancellationToken cancellationToken);
        protected abstract UniTask OnCloseAsync(CancellationToken cancellationToken);
    }
}
