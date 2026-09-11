using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HikanyanLibrary.UITools
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class GradientImage : BaseMeshEffect
    {
        [SerializeField] private Gradient _gradient = new Gradient();
        [SerializeField] private GridLayoutGroup.Axis _axis = GridLayoutGroup.Axis.Vertical;

        // 推奨：RectTransformの範囲でtを計算（頂点分布に依存しない）
        [SerializeField] private bool _useRectTransformBounds = true;

        // 端を少し内側に寄せたい場合（角丸で端が潰れるなどの見た目調整用）
        [SerializeField] private float _boundsPadding = 0f;

        private readonly List<UIVertex> _triStream = new();
        private readonly List<float> _splitKeys = new();
        private readonly List<UIVertex> _sliceBuffer = new();

        /// <summary>
        /// Dot等から差し替えるためのAPI
        /// </summary>
        public void SetGradient(Gradient gradient)
        {
            // null対策（呼び出し側がnullを渡しても落ちないようにする）
            _gradient = gradient ?? new Gradient();

            // 変更を即座に反映
            SetDirty();
        }

        public void SetAxis(GridLayoutGroup.Axis axis)
        {
            _axis = axis;
            SetDirty();
        }

        public void SetUseRectTransformBounds(bool useRectBounds)
        {
            _useRectTransformBounds = useRectBounds;
            SetDirty();
        }

        public void SetBoundsPadding(float padding)
        {
            _boundsPadding = Mathf.Max(0f, padding);
            SetDirty();
        }

        private void SetDirty()
        {
            if (!isActiveAndEnabled) return;
            if (graphic == null) return;

            graphic.SetVerticesDirty();
            graphic.SetMaterialDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh == null) return;

            _triStream.Clear();
            vh.GetUIVertexStream(_triStream);
            if (_triStream.Count < 3) return;

            int axisIndex = (int)_axis; // 0:X 1:Y

            // 1) キー時間（0,1以外）を集めてソート（重複排除）
            CollectSplitKeys(_splitKeys);

            // 2) キー位置で三角形をスライスして分割（中間色が安定）
            for (int k = 0; k < _splitKeys.Count; k++)
            {
                float plane = GetPlaneValue(_splitKeys[k], axisIndex);
                SliceTrianglesInPlace(_triStream, axisIndex, plane);
            }

            // 3) 最終的に頂点色を設定（Rect基準が安定）
            ApplyVertexColors(_triStream, axisIndex);

            vh.Clear();
            vh.AddUIVertexTriangleStream(_triStream);
        }

        private void CollectSplitKeys(List<float> keys)
        {
            keys.Clear();

            // colorKeys / alphaKeys の time を集める（0と1は除外）
            if (_gradient != null)
            {
                foreach (var ck in _gradient.colorKeys)
                {
                    if (ck.time > 0f && ck.time < 1f) keys.Add(ck.time);
                }
                foreach (var ak in _gradient.alphaKeys)
                {
                    if (ak.time > 0f && ak.time < 1f) keys.Add(ak.time);
                }
            }

            // 重複排除（許容誤差つき）＆昇順
            keys.Sort();
            for (int i = keys.Count - 1; i > 0; i--)
            {
                if (Mathf.Abs(keys[i] - keys[i - 1]) < 1e-4f)
                    keys.RemoveAt(i);
            }
        }

        private float GetPlaneValue(float keyT, int axisIndex)
        {
            if (_useRectTransformBounds && graphic != null)
            {
                Rect r = graphic.rectTransform.rect;
                float min = (axisIndex == 0) ? (r.xMin + _boundsPadding) : (r.yMin + _boundsPadding);
                float max = (axisIndex == 0) ? (r.xMax - _boundsPadding) : (r.yMax - _boundsPadding);
                return Mathf.Lerp(min, max, keyT);
            }
            else
            {
                // フォールバック：頂点min/max
                float min = _triStream[0].position[axisIndex];
                float max = min;
                for (int i = 1; i < _triStream.Count; i++)
                {
                    float v = _triStream[i].position[axisIndex];
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
                return Mathf.Lerp(min, max, keyT);
            }
        }

        private void ApplyVertexColors(List<UIVertex> stream, int axisIndex)
        {
            if (_gradient == null) return;

            float min, max;
            if (_useRectTransformBounds && graphic != null)
            {
                Rect r = graphic.rectTransform.rect;
                min = (axisIndex == 0) ? (r.xMin + _boundsPadding) : (r.yMin + _boundsPadding);
                max = (axisIndex == 0) ? (r.xMax - _boundsPadding) : (r.yMax - _boundsPadding);
            }
            else
            {
                min = stream[0].position[axisIndex];
                max = min;
                for (int i = 1; i < stream.Count; i++)
                {
                    float v = stream[i].position[axisIndex];
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            }

            if (Mathf.Approximately(min, max)) return;

            for (int i = 0; i < stream.Count; i++)
            {
                var v = stream[i];
                float t = Mathf.InverseLerp(min, max, v.position[axisIndex]);
                Color g = _gradient.Evaluate(t);

                // 元の頂点色（ImageのTint等）を保持するため乗算
                Color baseC = v.color;
                v.color = new Color(
                    baseC.r * g.r,
                    baseC.g * g.g,
                    baseC.b * g.b,
                    baseC.a * g.a
                );

                stream[i] = v;
            }
        }

        /// <summary>
        /// 三角形列（3頂点=1三角形）を plane でスライスし、交差する三角形を分割する
        /// </summary>
        private void SliceTrianglesInPlace(List<UIVertex> triStream, int axisIndex, float planeValue)
        {
            var outStream = _sliceBuffer;
            outStream.Clear();

            for (int i = 0; i < triStream.Count; i += 3)
            {
                UIVertex a = triStream[i + 0];
                UIVertex b = triStream[i + 1];
                UIVertex c = triStream[i + 2];

                SplitTriangleByPlane(a, b, c, axisIndex, planeValue, outStream);
            }

            triStream.Clear();
            triStream.AddRange(outStream);
        }

        private static void SplitTriangleByPlane(
            UIVertex a, UIVertex b, UIVertex c,
            int axisIndex, float plane,
            List<UIVertex> outStream)
        {
            float da = a.position[axisIndex] - plane;
            float db = b.position[axisIndex] - plane;
            float dc = c.position[axisIndex] - plane;

            const float eps = 1e-6f;
            da = Mathf.Abs(da) < eps ? 0f : da;
            db = Mathf.Abs(db) < eps ? 0f : db;
            dc = Mathf.Abs(dc) < eps ? 0f : dc;

            bool aPos = da >= 0f;
            bool bPos = db >= 0f;
            bool cPos = dc >= 0f;

            int posCount = (aPos ? 1 : 0) + (bPos ? 1 : 0) + (cPos ? 1 : 0);

            if (posCount == 0 || posCount == 3)
            {
                outStream.Add(a); outStream.Add(b); outStream.Add(c);
                return;
            }

            if (posCount == 1)
            {
                UIVertex p, n1, n2;
                if (aPos) { p = a; n1 = b; n2 = c; }
                else if (bPos) { p = b; n1 = c; n2 = a; }
                else { p = c; n1 = a; n2 = b; }

                UIVertex i1 = Intersect(p, n1, axisIndex, plane);
                UIVertex i2 = Intersect(p, n2, axisIndex, plane);

                outStream.Add(p); outStream.Add(i1); outStream.Add(i2);

                outStream.Add(n1); outStream.Add(n2); outStream.Add(i2);
                outStream.Add(n1); outStream.Add(i2); outStream.Add(i1);
            }
            else // posCount == 2
            {
                UIVertex n, p1, p2;
                if (!aPos) { n = a; p1 = b; p2 = c; }
                else if (!bPos) { n = b; p1 = c; p2 = a; }
                else { n = c; p1 = a; p2 = b; }

                UIVertex i1 = Intersect(p1, n, axisIndex, plane);
                UIVertex i2 = Intersect(p2, n, axisIndex, plane);

                outStream.Add(p1); outStream.Add(p2); outStream.Add(i2);
                outStream.Add(p1); outStream.Add(i2); outStream.Add(i1);

                outStream.Add(n); outStream.Add(i1); outStream.Add(i2);
            }
        }

        private static UIVertex Intersect(UIVertex vPos, UIVertex vNeg, int axisIndex, float plane)
        {
            float p = vPos.position[axisIndex];
            float n = vNeg.position[axisIndex];

            float t = (Mathf.Approximately(p, n)) ? 0f : Mathf.InverseLerp(p, n, plane); // vPos -> vNeg
            return LerpVertex(vPos, vNeg, t);
        }

        private static UIVertex LerpVertex(in UIVertex a, in UIVertex b, float t)
        {
            UIVertex v = a;

            v.position = Vector3.LerpUnclamped(a.position, b.position, t);

            v.uv0 = Vector4.LerpUnclamped(a.uv0, b.uv0, t);
            v.uv1 = Vector4.LerpUnclamped(a.uv1, b.uv1, t);
            v.uv2 = Vector4.LerpUnclamped(a.uv2, b.uv2, t);
            v.uv3 = Vector4.LerpUnclamped(a.uv3, b.uv3, t);

            v.color = Color32.Lerp(a.color, b.color, t);
            v.normal = Vector3.LerpUnclamped(a.normal, b.normal, t);
            v.tangent = Vector4.LerpUnclamped(a.tangent, b.tangent, t);

            return v;
        }
    }
}
