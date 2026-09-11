using System;

namespace HikanyanLibrary.UISystem
{
    public interface IUIArguments<TArgs> where TArgs : Parameter { }
    /// <summary>A compile-time association between a catalog id, presenter and its arguments.</summary>
    public sealed class UIKey<TArgs> where TArgs : Parameter
    {
        public string Id { get; }
        public Type PresenterType { get; }
        private UIKey(string id, Type presenterType)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("UI id is required.");
            Id = id;
            PresenterType = presenterType;
        }
        public static UIKey<TArgs> For<TPresenter>(string id) where TPresenter : UINodeBase, IUIArguments<TArgs> => new(id, typeof(TPresenter));
    }
    public sealed class UIDialogKey<TArgs, TResult> where TArgs : Parameter
    {
        internal UIKey<TArgs> Screen { get; }
        private UIDialogKey(UIKey<TArgs> screen) => Screen = screen;
        public static UIDialogKey<TArgs, TResult> For<TPresenter>(string id)
            where TPresenter : UINodeBase, IUIArguments<TArgs>, IUIResult<TResult> => new(UIKey<TArgs>.For<TPresenter>(id));
    }
}
