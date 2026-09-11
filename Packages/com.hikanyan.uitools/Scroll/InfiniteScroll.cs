using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HikanyanLibrary.UITools
{
    public class InfiniteScroll : UIBehaviour
    {
        [SerializeField] private RectTransform itemPrototype;

        [SerializeField, Range(0, 30)]
        private int instantiateItemCount = 9;

        [SerializeField] private Direction direction = Direction.Vertical;

        [Header("Layout")]
        [SerializeField] private RectOffset padding;
        [SerializeField] private float spacing = 0f;
        [SerializeField] private TextAnchor childAlignment = TextAnchor.UpperLeft;

        [Header("Recycle")]
        [SerializeField, Min(0f)] private float recycleBufferScreens = 2f; // 何画面分外に出たらリサイクルするか
        [SerializeField, Min(1)] private int maxRecyclePerFrame = 4;       // 無限ループ防止：1フレームの最大リサイクル回数

        public OnItemPositionChange onUpdateItem = new OnItemPositionChange();

        [System.NonSerialized]
        public LinkedList<RectTransform> itemList = new LinkedList<RectTransform>();

        // 先頭アイテムの論理インデックス（itemList.First がこの番号）
        protected int currentItemNo = 0;

        public enum Direction
        {
            Vertical,
            Horizontal,
        }

        // cache
        private RectTransform _rectTransform;
        protected RectTransform rectTransform
        {
            get
            {
                if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }

        private ScrollRect _scrollRect;
        private RectTransform _viewport;

        // 元コード互換：prototype基準の情報（ItemControllerLoop が参照している想定）
        private float _itemScale = -1f;
        public float itemScale
        {
            get
            {
                if (itemPrototype != null && _itemScale < 0f)
                {
                    _itemScale = direction == Direction.Vertical ? itemPrototype.rect.height : itemPrototype.rect.width;
                }
                return _itemScale;
            }
        }

        // 固定セルではないが、既存のSnap計算がStepを使っているため残す（Snapの当たり判定用）
        public float Step => itemScale + spacing;

        // 外部参照
        public Direction ScrollDirection => direction;
        public RectOffset Padding => padding;
        public float Spacing => spacing;
        public TextAnchor ChildAlignment => childAlignment;

        protected override void Awake()
        {
            base.Awake();
            padding ??= new RectOffset(0, 0, 0, 0);
        }

        private bool _initialized;
        private List<IInfiniteScroll> _controllers;

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureInitialized();
        }

        protected override void Start()
        {
            base.Start();
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            if (!isActiveAndEnabled) return;

            if (itemPrototype == null)
            {
                Debug.LogError($"{nameof(InfiniteScroll)}: itemPrototype が未設定です。", this);
                return;
            }

            _scrollRect = GetComponentInParent<ScrollRect>();
            if (_scrollRect == null)
            {
                Debug.LogError($"{nameof(InfiniteScroll)}: 親に ScrollRect が見つかりません。", this);
                return;
            }

            _viewport = _scrollRect.viewport != null ? _scrollRect.viewport : _scrollRect.GetComponent<RectTransform>();

            _scrollRect.horizontal = direction == Direction.Horizontal;
            _scrollRect.vertical = direction == Direction.Vertical;
            _scrollRect.content = rectTransform;

            // controller 取得
            var controllers = _controllers = GetComponents<MonoBehaviour>()
                .Where(x => x is IInfiniteScroll)
                .Cast<IInfiniteScroll>()
                .ToList();

            // prototype を非表示（複製元）
            itemPrototype.gameObject.SetActive(false);

            currentItemNo = 0;
            itemList.Clear();

            // 初期生成：padding から積み上げ
            float pos = GetPaddingMainStart();

            for (int i = 0; i < instantiateItemCount; i++)
            {
                var item = Instantiate(itemPrototype);
                item.SetParent(transform, false);
                item.name = i.ToString();

                SetItemMainPos(item, pos);

                itemList.AddLast(item);
                item.gameObject.SetActive(true);

                // 初期表示更新（ここで item の sizeDelta が変わる可能性あり）
                BindItem(i, item.gameObject);

                // サイズ確定後の実寸で積み上げ
                pos += GetItemMainSize(item) + spacing;
            }

            for (int c = 0; c < controllers.Count; c++)
            {
                controllers[c].OnPostSetupItems();
            }

            // 実サイズで整列（Bindでサイズが変わった前提）
            RequestRelayout(forceLayoutRebuild: true);

            _initialized = true;
        }

        void Update()
        {
            if (itemList.First == null || _viewport == null) return;

            float viewSize = GetViewportMainSize();
            if (viewSize <= 0f) return;

            float buffer = viewSize * recycleBufferScreens;

            // content 内座標系での viewport 範囲（主軸）
            float viewStart = GetViewStartMain();
            float viewEnd = viewStart + viewSize;

            int guard = 0;

            // ==========================
            // 前方向（下/右）へ：先頭を末尾に回す
            // ==========================
            while (guard++ < maxRecyclePerFrame && TryGetFirst(out var first) && TryGetLast(out var last))
            {
                float firstEnd = GetItemMainStart(first) + GetItemMainSize(first);
                if (firstEnd >= viewStart - buffer) break;

                float lastEnd = GetItemMainStart(last) + GetItemMainSize(last);

                var recycled = itemList.First;
                itemList.Remove(recycled);
                itemList.AddLast(recycled);

                currentItemNo++;
                int newIndex = currentItemNo + (instantiateItemCount - 1);

                float newStart = lastEnd + spacing;
                SetItemMainPos(first, newStart);

                BindItem(newIndex, first.gameObject);

                FixStackFromNode(itemList.Last);
            }

            guard = 0;

            // ==========================
            // 後方向（上/左）へ：末尾を先頭に回す
            // ==========================
            while (guard++ < maxRecyclePerFrame && TryGetFirst(out var first2) && TryGetLast(out var last2))
            {
                float lastStart = GetItemMainStart(last2);
                if (lastStart <= viewEnd + buffer) break;

                float firstStart = GetItemMainStart(first2);
                float lastSize = GetItemMainSize(last2);

                var recycled = itemList.Last;
                itemList.Remove(recycled);
                itemList.AddFirst(recycled);

                currentItemNo--;
                int newIndex = currentItemNo;

                float newStart = firstStart - spacing - lastSize;
                SetItemMainPos(last2, newStart);

                BindItem(newIndex, last2.gameObject);

                FixStackFromNode(itemList.First);
            }
        }

        // =========================
        // Public API
        // =========================

        public void RequestRelayout(bool forceLayoutRebuild)
        {
            RepositionAll(forceLayoutRebuild);
        }

        private void BindItem(int index, GameObject item)
        {
            if (_controllers != null)
                foreach (var controller in _controllers) controller.OnUpdateItem(index, item);
            onUpdateItem.Invoke(index, item);
        }

        public void RefreshItems()
        {
            var index = currentItemNo;
            foreach (var item in itemList) BindItem(index++, item.gameObject);
            RequestRelayout(true);
        }

        // =========================
        // Layout
        // =========================

        private void RepositionAll(bool forceLayoutRebuild)
        {
            if (itemList == null || itemList.Count == 0) return;

            float pos = GetPaddingMainStart();

            var node = itemList.First;
            while (node != null)
            {
                var rt = node.Value;
                if (rt != null)
                {
                    SetItemMainPos(rt, pos);
                    pos += GetItemMainSize(rt) + spacing;
                }
                node = node.Next;
            }

            if (forceLayoutRebuild)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        private void FixStackFromNode(LinkedListNode<RectTransform> startNode)
        {
            if (startNode == null) return;

            var node = startNode;
            while (node != null && node.Next != null)
            {
                var a = node.Value;
                var b = node.Next.Value;
                if (a == null || b == null)
                {
                    node = node.Next;
                    continue;
                }

                float aStart = GetItemMainStart(a);
                float aEnd = aStart + GetItemMainSize(a);

                float bStart = GetItemMainStart(b);
                float desired = aEnd + spacing;

                if (!Mathf.Approximately(bStart, desired))
                {
                    SetItemMainPos(b, desired);
                }

                node = node.Next;
            }
        }

        // =========================
        // Helpers
        // =========================

        private bool TryGetFirst(out RectTransform first)
        {
            first = itemList.First != null ? itemList.First.Value : null;
            return first != null;
        }

        private bool TryGetLast(out RectTransform last)
        {
            last = itemList.Last != null ? itemList.Last.Value : null;
            return last != null;
        }

        private float GetPaddingMainStart()
        {
            return direction == Direction.Vertical ? padding.top : padding.left;
        }

        private float GetViewportMainSize()
        {
            return direction == Direction.Vertical ? _viewport.rect.height : _viewport.rect.width;
        }

        private float GetViewStartMain()
        {
            if (direction == Direction.Vertical)
            {
                return rectTransform.anchoredPosition.y;
            }
            else
            {
                return -rectTransform.anchoredPosition.x;
            }
        }

        private float GetItemMainSize(RectTransform item)
        {
            return direction == Direction.Vertical ? item.rect.height : item.rect.width;
        }

        private float GetItemMainStart(RectTransform item)
        {
            return direction == Direction.Vertical ? -item.anchoredPosition.y : item.anchoredPosition.x;
        }

        private void SetItemMainPos(RectTransform item, float mainPos)
        {
            float contentW = rectTransform.rect.width;
            float contentH = rectTransform.rect.height;

            float itemW = item.rect.width;
            float itemH = item.rect.height;

            if (direction == Direction.Vertical)
            {
                float x = CalculateCrossAxisOffset_Vertical(contentW, itemW);
                item.anchoredPosition = new Vector2(x, -mainPos);
            }
            else
            {
                float y = CalculateCrossAxisOffset_Horizontal(contentH, itemH);
                item.anchoredPosition = new Vector2(mainPos, y);
            }
        }

        private float CalculateCrossAxisOffset_Vertical(float contentW, float itemW)
        {
            float left = padding.left;
            float right = padding.right;
            float usable = Mathf.Max(0f, contentW - left - right);

            var h = GetHorizontalAlign(childAlignment);

            if (h == HorizontalAlign.Left) return left;
            if (h == HorizontalAlign.Center) return left + (usable - itemW) * 0.5f;
            return contentW - right - itemW;
        }

        private float CalculateCrossAxisOffset_Horizontal(float contentH, float itemH)
        {
            float top = padding.top;
            float bottom = padding.bottom;
            float usable = Mathf.Max(0f, contentH - top - bottom);

            var v = GetVerticalAlign(childAlignment);

            if (v == VerticalAlign.Top) return -top;
            if (v == VerticalAlign.Middle) return -(top + (usable - itemH) * 0.5f);
            return -(contentH - bottom - itemH);
        }

        private enum HorizontalAlign { Left, Center, Right }
        private enum VerticalAlign { Top, Middle, Bottom }

        private static HorizontalAlign GetHorizontalAlign(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft:
                    return HorizontalAlign.Left;
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    return HorizontalAlign.Center;
                default:
                    return HorizontalAlign.Right;
            }
        }

        private static VerticalAlign GetVerticalAlign(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    return VerticalAlign.Top;
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    return VerticalAlign.Middle;
                default:
                    return VerticalAlign.Bottom;
            }
        }
        public void CompensateToKeepItemWorldCenter(RectTransform itemRt, Vector3 worldCenterBefore)
        {
            if (itemRt == null) return;

            // サイズ変更後の中心（World）
            Vector3 worldCenterAfter = itemRt.TransformPoint(itemRt.rect.center);

            // 変化分を content（=rectTransform）に戻す
            Vector3 deltaWorld = worldCenterBefore - worldCenterAfter;
            Vector3 deltaLocal = rectTransform.InverseTransformVector(deltaWorld);

            var ap = rectTransform.anchoredPosition;

            if (direction == Direction.Vertical)
            {
                // anchoredPosition.y は「上が+」でスクロール方向と符号がズレるので、local.yをそのまま加算でOK
                rectTransform.anchoredPosition = new Vector2(ap.x, ap.y + deltaLocal.y);
            }
            else
            {
                rectTransform.anchoredPosition = new Vector2(ap.x + deltaLocal.x, ap.y);
            }
        }
        [System.Serializable]
        public class OnItemPositionChange : UnityEngine.Events.UnityEvent<int, GameObject> { }
    }
}
