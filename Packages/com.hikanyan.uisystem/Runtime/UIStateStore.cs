using System;
using System.Collections.Generic;

namespace HikanyanLibrary.UISystem
{
    /// <summary>Optional presentation state. Save data and domain rules remain in game services.</summary>
    public sealed class UIStateStore<T>
    {
        public T Value { get; private set; }
        public event Action<T> Changed;
        public UIStateStore(T initial) => Value = initial;
        public void Set(T value)
        {
            if (EqualityComparer<T>.Default.Equals(Value, value)) return;
            Value = value;
            Changed?.Invoke(value);
        }
    }

    /// <summary>Register unsubscribe actions once per binding. Disposal is idempotent.</summary>
    public sealed class UIBindings : IDisposable
    {
        private readonly List<Action> _cleanup = new();
        private bool _disposed;
        public void Add(Action unsubscribe)
        {
            if (unsubscribe == null) throw new ArgumentNullException(nameof(unsubscribe));
            if (_disposed) { unsubscribe(); return; }
            _cleanup.Add(unsubscribe);
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            List<Exception> errors = null;
            for (var i = _cleanup.Count - 1; i >= 0; i--)
                try { _cleanup[i](); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
            _cleanup.Clear();
            if (errors != null) throw new AggregateException(errors);
        }
    }
}
