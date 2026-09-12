using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace HikanyanLibrary.UISystem.Samples.Tests
{
    public sealed class SampleTests
    {
        [UnityTest] public IEnumerator PurchaseUpdatesGameStateAndCancelRestoresFocus()
        {
            var demo = new GameObject("demo");
            demo.AddComponent<MinimalDemo>();
            try
            {
                yield return null;
                var inventory = Object.FindFirstObjectByType<InventoryPresenter>();
                Assert.IsNotNull(inventory);
                var view = inventory.GetComponent<DemoView>();
                StringAssert.Contains("100", view.Body.text);
                view.Primary.onClick.Invoke();
                yield return null;
                var dialog = Object.FindFirstObjectByType<ConfirmPresenter>();
                Assert.IsNotNull(dialog);
                dialog.GetComponent<DemoView>().Primary.onClick.Invoke();
                yield return null;
                StringAssert.Contains("90", view.Body.text);
                view.Primary.onClick.Invoke();
                yield return null;
                dialog = Object.FindFirstObjectByType<ConfirmPresenter>();
                ExecuteEvents.Execute(dialog.GetComponent<DemoView>().Primary.gameObject,
                    new BaseEventData(EventSystem.current), ExecuteEvents.cancelHandler);
                yield return null;
                Assert.IsNull(Object.FindFirstObjectByType<ConfirmPresenter>());
                StringAssert.Contains("90", view.Body.text);
                Assert.AreEqual(view.Primary.gameObject, EventSystem.current.currentSelectedGameObject);
            }
            finally { Object.DestroyImmediate(demo); }
            yield return null;
        }
    }
}
