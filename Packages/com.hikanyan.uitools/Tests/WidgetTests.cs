using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HikanyanLibrary.UITools.Tests
{
    public sealed class CountingButton : ButtonCommon
    {
        public int Clicks;
        protected override void OnClicked(PointerEventData data) => Clicks++;
    }
    public sealed class CountingScroll : MonoBehaviour, IInfiniteScroll
    {
        public int Binds;
        public void OnUpdateItem(int index, GameObject item) => Binds++;
        public void OnPostSetupItems() { }
    }
    public sealed class WidgetTests
    {
        [Test] public void ThemeCacheInvalidatesWhenDataChanges()
        {
            var theme = ScriptableObject.CreateInstance<UITheme>();
            try
            {
                theme.SetColor(UIColorRole.ButtonShadow, Color.red);
                Assert.AreEqual(Color.red, theme.Get(UIColorRole.ButtonShadow));
                theme.SetColor(UIColorRole.ButtonShadow, Color.blue);
                Assert.AreEqual(Color.blue, theme.Get(UIColorRole.ButtonShadow));
            }
            finally { Object.DestroyImmediate(theme); }
        }
        [Test] public void BinderConnectsToProviderCreatedLater()
        {
            var image = new GameObject("image", typeof(RectTransform), typeof(Image), typeof(ThemeColorBinder));
            var provider = new GameObject("provider");
            var theme = ScriptableObject.CreateInstance<UITheme>();
            try
            {
                theme.SetColor(UIColorRole.ButtonShadow, Color.green);
                provider.AddComponent<UIThemeProvider>().SetTheme(theme);
                Assert.AreEqual(Color.green, image.GetComponent<Image>().color);
                theme.SetColor(UIColorRole.ButtonShadow, Color.blue);
                Assert.AreEqual(Color.blue, image.GetComponent<Image>().color);
            }
            finally { Object.DestroyImmediate(image); Object.DestroyImmediate(provider); Object.DestroyImmediate(theme); }
        }
        [Test] public void ButtonHandlesSubmitAndRespectsCanvasGroup()
        {
            var parent = new GameObject("parent", typeof(CanvasGroup));
            var go = new GameObject("button", typeof(RectTransform), typeof(Button));
            go.transform.SetParent(parent.transform);
            var events = new GameObject("events", typeof(EventSystem));
            try
            {
                var common = go.AddComponent<CountingButton>();
                var button = go.GetComponent<Button>();
                button.OnPointerClick(new PointerEventData(events.GetComponent<EventSystem>()) { button = PointerEventData.InputButton.Right });
                Assert.AreEqual(0, common.Clicks);
                button.OnSubmit(new BaseEventData(events.GetComponent<EventSystem>()));
                Assert.AreEqual(1, common.Clicks);
                parent.GetComponent<CanvasGroup>().interactable = false;
                button.OnSubmit(new BaseEventData(events.GetComponent<EventSystem>()));
                Assert.AreEqual(1, common.Clicks);
            }
            finally { Object.DestroyImmediate(parent); Object.DestroyImmediate(events); }
        }
        [UnityTest] public IEnumerator RecycledCellsNotifyInterfaceAndReuseLinkedNodes()
        {
            var root = new GameObject("scroll", typeof(RectTransform));
            root.SetActive(false);
            var rootRect = (RectTransform)root.transform; rootRect.sizeDelta = new Vector2(100, 100);
            var scrollRect = root.AddComponent<ScrollRect>();
            var content = new GameObject("content", typeof(RectTransform)); content.transform.SetParent(root.transform, false);
            var contentRect = (RectTransform)content.transform; contentRect.sizeDelta = new Vector2(100, 1000);
            var prototype = new GameObject("prototype", typeof(RectTransform)); prototype.transform.SetParent(content.transform, false);
            ((RectTransform)prototype.transform).sizeDelta = new Vector2(100, 100);
            var controller = content.AddComponent<CountingScroll>();
            var scroll = content.AddComponent<InfiniteScroll>();
            void Set(string field, object value) => typeof(InfiniteScroll).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroll, value);
            Set("itemPrototype", prototype.transform);
            Set("instantiateItemCount", 3);
            Set("recycleBufferScreens", 0f);
            scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            scrollRect.inertia = false;
            try
            {
                root.SetActive(true);
                var nodes = new HashSet<LinkedListNode<RectTransform>>();
                for (var node = scroll.itemList.First; node != null; node = node.Next) nodes.Add(node);
                var initialBinds = controller.Binds;
                contentRect.anchoredPosition = new Vector2(0, 500);
                yield return null;
                Assert.Greater(controller.Binds, initialBinds);
                for (var node = scroll.itemList.First; node != null; node = node.Next) Assert.IsTrue(nodes.Contains(node));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
