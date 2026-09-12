using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using HikanyanLibrary.UISystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HikanyanLibrary.UISystem.Samples
{
    /// <summary>A self-contained example; no Addressables catalog, TMP fonts or external game data are required.</summary>
    public sealed class MinimalDemo : MonoBehaviour
    {
        private UIManager _ui;
        private UICatalog _catalog;
        private GameObject _pagePrefab, _dialogPrefab, _events;
        private readonly UIStateStore<int> _coins = new(100);
        private static readonly UIKey<InventoryArgs> Inventory = UIKey<InventoryArgs>.For<InventoryPresenter>("Inventory");
        public static readonly UIDialogKey<ConfirmArgs, bool> Confirm = UIDialogKey<ConfirmArgs, bool>.For<ConfirmPresenter>("Confirm");

        private void Start() => RunAsync(this.GetCancellationTokenOnDestroy()).Forget();
        private async UniTask RunAsync(CancellationToken token)
        {
            var system = new GameObject("UI System", typeof(UIManager), typeof(UIBootstrap));
            _ui = system.GetComponent<UIManager>();
            if (EventSystem.current == null)
            {
                _events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                _events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            _catalog = ScriptableObject.CreateInstance<UICatalog>();
            _ui.Catalog = _catalog;
            _pagePrefab = BuildPanel("Inventory prototype", false);
            var inventory = _pagePrefab.AddComponent<InventoryPresenter>();
            _dialogPrefab = BuildPanel("Confirm prototype", true);
            var confirm = _dialogPrefab.AddComponent<ConfirmPresenter>();
            Add("Inventory", inventory, UILayer.Page, UIReuse.KeepAlive);
            Add("Confirm", confirm, UILayer.Modal, UIReuse.Destroy);
            await _ui.OpenAsync(Inventory, new InventoryArgs(_coins, _ui), token);
        }
        private void Add(string id, UINodeBase prefab, UILayer layer, UIReuse reuse)
        {
            var definition = ScriptableObject.CreateInstance<UIDefinition>();
            definition.Id = id; definition.Prefab = prefab; definition.Layer = layer; definition.Reuse = reuse;
            definition.CloseOnBack = layer == UILayer.Modal;
            _catalog.Screens.Add(definition);
        }
        private static GameObject BuildPanel(string name, bool dialog)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.SetActive(false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760, 480);
            panel.GetComponent<Image>().color = new Color(0.09f, 0.13f, 0.21f);
            var view = panel.AddComponent<DemoView>();
            view.Heading = Label(panel.transform, dialog ? "Confirm purchase" : "Inventory", new Vector2(0, 155), 36);
            view.Body = Label(panel.transform, "", new Vector2(0, 35), 28);
            view.Primary = MakeButton(panel.transform, dialog ? "Confirm" : "Buy potion (10 coins)", new Vector2(0, -80));
            view.Secondary = MakeButton(panel.transform, dialog ? "Cancel" : "Add 10 coins", new Vector2(0, -160));
            return panel;
        }
        private static Text Label(Transform parent, string value, Vector2 position, int size)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(690, 75); rect.anchoredPosition = position;
            var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.text = value; text.raycastTarget = false;
            return text;
        }
        private static Button MakeButton(Transform parent, string title, Vector2 position)
        {
            var go = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(540, 60); rect.anchoredPosition = position;
            go.GetComponent<Image>().color = new Color(0.12f, 0.35f, 0.58f);
            Label(go.transform, title, Vector2.zero, 25);
            return go.GetComponent<Button>();
        }
        private void OnDestroy()
        {
            if (_ui != null) Destroy(_ui.gameObject);
            if (_events != null) Destroy(_events);
            if (_pagePrefab != null) Destroy(_pagePrefab);
            if (_dialogPrefab != null) Destroy(_dialogPrefab);
            if (_catalog != null)
            {
                foreach (var screen in _catalog.Screens) Destroy(screen);
                Destroy(_catalog);
            }
        }
    }
    public sealed class DemoView : MonoBehaviour
    {
        public Text Heading, Body;
        public Button Primary, Secondary;
    }
    public sealed class InventoryArgs : Parameter
    {
        public UIStateStore<int> Coins { get; }
        public UIManager UI { get; }
        public InventoryArgs(UIStateStore<int> coins, UIManager ui) { Coins = coins; UI = ui; }
    }
    public sealed class InventoryPresenter : PresenterBase<DemoView, InventoryArgs>
    {
        private bool _buying;
        protected override void OnBind()
        {
            Render(Model.Coins.Value);
            var coins = Model.Coins;
            coins.Changed += Render;
            Bindings.Add(() => coins.Changed -= Render);
            View.Primary.onClick.AddListener(Buy);
            View.Secondary.onClick.AddListener(AddCoins);
            Bindings.Add(() => View.Primary.onClick.RemoveListener(Buy));
            Bindings.Add(() => View.Secondary.onClick.RemoveListener(AddCoins));
        }
        private void Render(int value) => View.Body.text = $"Coins: {value}\nGame state lives outside the view.";
        private void AddCoins() => Model.Coins.Set(Model.Coins.Value + 10);
        private void Buy() => BuyAsync(BindingToken).Forget();
        private async UniTask BuyAsync(CancellationToken token)
        {
            if (_buying) return;
            _buying = true;
            try
            {
                if (Model.Coins.Value < 10) { View.Body.text = "Not enough coins."; return; }
                if (await Model.UI.ShowDialogAsync(MinimalDemo.Confirm, new ConfirmArgs("Spend 10 coins on a potion?"), token))
                    Model.Coins.Set(Model.Coins.Value - 10);
            }
            catch (OperationCanceledException) { }
            finally { _buying = false; }
        }
    }
    public sealed class ConfirmArgs : Parameter
    {
        public string Message { get; }
        public ConfirmArgs(string message) => Message = message;
    }
    public sealed class ConfirmPresenter : DialogPresenterBase<DemoView, ConfirmArgs, bool>
    {
        protected override void OnDialogBind()
        {
            View.Body.text = Model.Message;
            View.Primary.onClick.AddListener(Confirm);
            View.Secondary.onClick.AddListener(Cancel);
            Bindings.Add(() => View.Primary.onClick.RemoveListener(Confirm));
            Bindings.Add(() => View.Secondary.onClick.RemoveListener(Cancel));
        }
        private void Confirm() => CompleteResult(true);
        private void Cancel() => CompleteResult(false);
    }
}
