using UnityEngine;
using UnityEngine.UI;

namespace HikanyanLibrary.UISystem
{
    [DefaultExecutionOrder(-11000)]
    [RequireComponent(typeof(UIManager))]
    public sealed class UIBootstrap : MonoBehaviour
    {
        private void Awake() => Initialize();
        public void Initialize()
        {
            var manager = GetComponent<UIManager>();
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                go.transform.SetParent(transform, false);
                canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            var safe = canvas.transform.Find("SafeArea") as RectTransform;
            if (safe == null)
            {
                safe = CreateRect("SafeArea", canvas.transform);
                safe.gameObject.AddComponent<UISafeArea>();
            }
            manager.DefaultRoot = safe;
            var scope = safe.GetComponent<UIScope>() ?? safe.gameObject.AddComponent<UIScope>();
            scope.Initialize(manager);
            foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
            {
                var root = safe.Find(layer.ToString()) ?? CreateRect(layer.ToString(), safe);
                manager.SetLayerRoot(layer, root);
            }
        }
        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
