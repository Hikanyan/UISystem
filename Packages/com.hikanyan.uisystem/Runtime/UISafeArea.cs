using UnityEngine;

namespace HikanyanLibrary.UISystem
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class UISafeArea : MonoBehaviour
    {
        private Rect _last;
        private Vector2Int _size;
        private void OnEnable() { _size = default; LateUpdate(); }
        private void LateUpdate()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var area = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (_last == area && _size == size) return;
            _last = area; _size = size;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
            rect.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
