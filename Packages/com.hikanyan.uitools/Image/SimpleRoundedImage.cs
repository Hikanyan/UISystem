using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

namespace HikanyanLibrary.UITools.Editor
{
    /// <summary>
    /// UGUI Image を角丸表示するための簡易実装。
    /// シェーダやマスクではなく「メッシュを自前生成」して形状を作る。
    /// 追加機能：三角形（正三角形）モード。
    /// 追加機能：Line > 0 のとき「内側をくり抜いた枠」を生成する。
    /// </summary>
    public class SimpleRoundedImage : Image
    {
        private const int MaxTriangleNum = 20;
        private const int MinTriangleNum = 1;

        public enum ShapeType
        {
            RoundedRect,
            Triangle
        }

        //[Header("Shape")]
        public ShapeType Shape = ShapeType.RoundedRect;

        //[Header("RoundedRect")]
        /// <summary>角丸の半径（RectTransformローカル座標系）</summary>
        public float Radius = 8f;

        /// <summary>各コーナーの1/4円を何個の三角形で近似するか</summary>
        [Range(MinTriangleNum, MaxTriangleNum)]
        public int TriangleNum = 6;

        //[Header("Border")]
        /// <summary>
        /// 枠の太さ（ローカル座標系、px相当）。
        /// 0 のとき塗りつぶし。0より大きいとき内側をくり抜いた枠を生成する。
        /// </summary>
        [Min(0f)]
        public float Line = 0f;

        //[Header("Triangle")]
        [Range(0f, 360f)]
        public float TriangleRotationDeg = 0f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Vector4 v = GetDrawingDimensions(false);

            Vector4 uv = overrideSprite != null
                ? DataUtility.GetOuterUV(overrideSprite)
                : new Vector4(0f, 0f, 1f, 1f);

            Color32 color32 = color;

            float width = v.z - v.x;
            float height = v.w - v.y;

            if (width <= 0f || height <= 0f)
                return;

            // Line > 0 の場合は「塗りつぶし」ではなく「枠メッシュ（内側くり抜き）」を生成
            if (Line > 0f)
            {
                switch (Shape)
                {
                    case ShapeType.Triangle:
                        PopulateTriangleBorder(vh, v, uv, color32, width, height);
                        return;

                    case ShapeType.RoundedRect:
                    default:
                        PopulateRoundedRectBorder(vh, v, uv, color32, width, height);
                        return;
                }
            }

            // Line == 0 は従来の塗りつぶし
            switch (Shape)
            {
                case ShapeType.Triangle:
                    PopulateTriangle(vh, v, uv, color32, width, height);
                    return;

                case ShapeType.RoundedRect:
                default:
                    PopulateRoundedRect(vh, v, uv, color32, width, height);
                    return;
            }
        }

        // =========================
        // Triangle (Fill)
        // =========================
        private void PopulateTriangle(VertexHelper vh, Vector4 v, Vector4 uv, Color32 color32, float width, float height)
        {
            float cx = (v.x + v.z) * 0.5f;
            float cy = (v.y + v.w) * 0.5f;

            float r = Mathf.Min(width, height) * 0.5f;
            float rot = TriangleRotationDeg * Mathf.Deg2Rad;

            for (int i = 0; i < 3; i++)
            {
                float a = rot + (i * 120f * Mathf.Deg2Rad);

                float px = cx + Mathf.Sin(a) * r;
                float py = cy + Mathf.Cos(a) * r;

                float u = Mathf.Lerp(uv.x, uv.z, (px - v.x) / width);
                float vv = Mathf.Lerp(uv.y, uv.w, (py - v.y) / height);

                vh.AddVert(new Vector3(px, py, 0f), color32, new Vector2(u, vv));
            }

            vh.AddTriangle(0, 1, 2);
        }

        // =========================
        // Triangle (Border)
        // =========================
        private void PopulateTriangleBorder(VertexHelper vh, Vector4 v, Vector4 uv, Color32 color32, float width, float height)
        {
            float cx = (v.x + v.z) * 0.5f;
            float cy = (v.y + v.w) * 0.5f;

            float outerR = Mathf.Min(width, height) * 0.5f;

            // 正三角形の内接/外接スケールは厳密には異なるが、
            // ここでは「中心からの半径」を同じだけ縮める簡易版（見た目重視）
            float innerR = Mathf.Max(0f, outerR - Line);

            // Lineが大きすぎる場合は塗りつぶしにフォールバック
            if (innerR <= 0f)
            {
                PopulateTriangle(vh, v, uv, color32, width, height);
                return;
            }

            float rot = TriangleRotationDeg * Mathf.Deg2Rad;

            // Outer 3 verts
            for (int i = 0; i < 3; i++)
            {
                float a = rot + (i * 120f * Mathf.Deg2Rad);
                float px = cx + Mathf.Sin(a) * outerR;
                float py = cy + Mathf.Cos(a) * outerR;

                float u = Mathf.Lerp(uv.x, uv.z, (px - v.x) / width);
                float vv = Mathf.Lerp(uv.y, uv.w, (py - v.y) / height);

                vh.AddVert(new Vector3(px, py, 0f), color32, new Vector2(u, vv));
            }

            // Inner 3 verts
            for (int i = 0; i < 3; i++)
            {
                float a = rot + (i * 120f * Mathf.Deg2Rad);
                float px = cx + Mathf.Sin(a) * innerR;
                float py = cy + Mathf.Cos(a) * innerR;

                float u = Mathf.Lerp(uv.x, uv.z, (px - v.x) / width);
                float vv = Mathf.Lerp(uv.y, uv.w, (py - v.y) / height);

                vh.AddVert(new Vector3(px, py, 0f), color32, new Vector2(u, vv));
            }

            // Connect as 3 quads (outer i -> outer i+1 -> inner i+1 -> inner i)
            // Outer: 0,1,2  Inner: 3,4,5
            for (int i = 0; i < 3; i++)
            {
                int o0 = i;
                int o1 = (i + 1) % 3;
                int in0 = 3 + i;
                int in1 = 3 + ((i + 1) % 3);

                // two triangles per quad
                vh.AddTriangle(o0, o1, in1);
                vh.AddTriangle(o0, in1, in0);
            }
        }

        // =========================
        // RoundedRect (Fill) - 既存
        // =========================
        private void PopulateRoundedRect(VertexHelper vh, Vector4 v, Vector4 uv, Color32 color32, float width, float height)
        {
            int triNum = Mathf.Clamp(TriangleNum, MinTriangleNum, MaxTriangleNum);

            float radius = Mathf.Max(0f, Radius);
            radius = Mathf.Min(radius, width * 0.5f);
            radius = Mathf.Min(radius, height * 0.5f);

            float uvRadiusX = radius / width;
            float uvRadiusY = radius / height;

            vh.AddVert(new Vector3(v.x, v.w - radius), color32, new Vector2(uv.x, uv.w - uvRadiusY)); // 0
            vh.AddVert(new Vector3(v.x, v.y + radius), color32, new Vector2(uv.x, uv.y + uvRadiusY)); // 1

            vh.AddVert(new Vector3(v.x + radius, v.w),          color32, new Vector2(uv.x + uvRadiusX, uv.w));              // 2
            vh.AddVert(new Vector3(v.x + radius, v.w - radius), color32, new Vector2(uv.x + uvRadiusX, uv.w - uvRadiusY)); // 3
            vh.AddVert(new Vector3(v.x + radius, v.y + radius), color32, new Vector2(uv.x + uvRadiusX, uv.y + uvRadiusY)); // 4
            vh.AddVert(new Vector3(v.x + radius, v.y),          color32, new Vector2(uv.x + uvRadiusX, uv.y));              // 5

            vh.AddVert(new Vector3(v.z - radius, v.w),          color32, new Vector2(uv.z - uvRadiusX, uv.w));              // 6
            vh.AddVert(new Vector3(v.z - radius, v.w - radius), color32, new Vector2(uv.z - uvRadiusX, uv.w - uvRadiusY)); // 7
            vh.AddVert(new Vector3(v.z - radius, v.y + radius), color32, new Vector2(uv.z - uvRadiusX, uv.y + uvRadiusY)); // 8
            vh.AddVert(new Vector3(v.z - radius, v.y),          color32, new Vector2(uv.z - uvRadiusX, uv.y));              // 9

            vh.AddVert(new Vector3(v.z, v.w - radius), color32, new Vector2(uv.z, uv.w - uvRadiusY)); // 10
            vh.AddVert(new Vector3(v.z, v.y + radius), color32, new Vector2(uv.z, uv.y + uvRadiusY)); // 11

            vh.AddTriangle(1, 0, 3);
            vh.AddTriangle(1, 3, 4);

            vh.AddTriangle(5, 2, 6);
            vh.AddTriangle(5, 6, 9);

            vh.AddTriangle(8, 7, 10);
            vh.AddTriangle(8, 10, 11);

            if (radius <= 0f)
                return;

            Vector2 c0 = new Vector2(v.z - radius, v.w - radius); // 右上
            Vector2 c1 = new Vector2(v.x + radius, v.w - radius); // 左上
            Vector2 c2 = new Vector2(v.x + radius, v.y + radius); // 左下
            Vector2 c3 = new Vector2(v.z - radius, v.y + radius); // 右下

            Vector2 u0 = new Vector2(uv.z - uvRadiusX, uv.w - uvRadiusY);
            Vector2 u1 = new Vector2(uv.x + uvRadiusX, uv.w - uvRadiusY);
            Vector2 u2 = new Vector2(uv.x + uvRadiusX, uv.y + uvRadiusY);
            Vector2 u3 = new Vector2(uv.z - uvRadiusX, uv.y + uvRadiusY);

            int center0 = 7;
            int center1 = 3;
            int center2 = 4;
            int center3 = 8;

            float delta = (Mathf.PI * 0.5f) / triNum;

            BuildCornerArc(vh, c0, u0, center0, radius, uvRadiusX, uvRadiusY, 0f,                delta, triNum, color32);
            BuildCornerArc(vh, c1, u1, center1, radius, uvRadiusX, uvRadiusY, Mathf.PI * 0.5f,   delta, triNum, color32);
            BuildCornerArc(vh, c2, u2, center2, radius, uvRadiusX, uvRadiusY, Mathf.PI,          delta, triNum, color32);
            BuildCornerArc(vh, c3, u3, center3, radius, uvRadiusX, uvRadiusY, Mathf.PI * 1.5f,   delta, triNum, color32);
        }

        // =========================
        // RoundedRect (Border) - 新規
        // =========================
        private void PopulateRoundedRectBorder(VertexHelper vh, Vector4 v, Vector4 uv, Color32 color32, float width, float height)
        {
            int triNum = Mathf.Clamp(TriangleNum, MinTriangleNum, MaxTriangleNum);

            float outerRadius = Mathf.Max(0f, Radius);
            outerRadius = Mathf.Min(outerRadius, width * 0.5f);
            outerRadius = Mathf.Min(outerRadius, height * 0.5f);

            float line = Mathf.Max(0f, Line);

            float innerXMin = v.x + line;
            float innerYMin = v.y + line;
            float innerXMax = v.z - line;
            float innerYMax = v.w - line;

            float innerW = innerXMax - innerXMin;
            float innerH = innerYMax - innerYMin;

            // 内側が成立しない（完全に潰れる）場合のみ塗りつぶしへ
            if (innerW <= 0f || innerH <= 0f)
            {
                PopulateRoundedRect(vh, v, uv, color32, width, height);
                return;
            }

            float innerRadius = Mathf.Max(0f, outerRadius - line);
            innerRadius = Mathf.Min(innerRadius, innerW * 0.5f);
            innerRadius = Mathf.Min(innerRadius, innerH * 0.5f);

            var outer = s_outerPoints;
            var inner = s_innerPoints;
            outer.Clear();
            inner.Clear();

            // ★ここが重要：内外とも「固定点数」で生成する
            BuildRoundedRectPerimeterPointsFixed(outer, v.x, v.y, v.z, v.w, outerRadius, triNum);
            BuildRoundedRectPerimeterPointsFixed(inner, innerXMin, innerYMin, innerXMax, innerYMax, innerRadius, triNum);

            int count = outer.Count;
            if (count < 4 || inner.Count != count)
            {
                // ここには基本来ない想定だが、保険
                PopulateRoundedRect(vh, v, uv, color32, width, height);
                return;
            }

            for (int i = 0; i < count; i++)
                AddVertWithUv(vh, outer[i], v, uv, width, height, color32);

            for (int i = 0; i < count; i++)
                AddVertWithUv(vh, inner[i], v, uv, width, height, color32);

            // 帯状に接続
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;

                int o0 = i;
                int o1 = next;
                int in0 = count + i;
                int in1 = count + next;

                vh.AddTriangle(o0, o1, in1);
                vh.AddTriangle(o0, in1, in0);
            }
        }


        // 使い回しバッファ（GC削減）
        private static readonly List<Vector2> s_outerPoints = new List<Vector2>(128);
        private static readonly List<Vector2> s_innerPoints = new List<Vector2>(128);

        private static void AddVertWithUv(VertexHelper vh, Vector2 pos, Vector4 v, Vector4 uv, float width, float height, Color32 color32)
        {
            float u = Mathf.Lerp(uv.x, uv.z, (pos.x - v.x) / width);
            float vv = Mathf.Lerp(uv.y, uv.w, (pos.y - v.y) / height);
            vh.AddVert(new Vector3(pos.x, pos.y, 0f), color32, new Vector2(u, vv));
        }

        /// <summary>
        /// 角丸矩形の外周点を、一定点数（4コーナー * (triNum+1) から重複除去）で生成する。
        /// 時計回りで一周する点列になる。
        /// </summary>
        private static void BuildRoundedRectPerimeterPoints(List<Vector2> points, float xMin, float yMin, float xMax, float yMax, float radius, int triNum)
        {
            points.Clear();

            float w = xMax - xMin;
            float h = yMax - yMin;

            if (w <= 0f || h <= 0f)
                return;

            radius = Mathf.Clamp(radius, 0f, Mathf.Min(w, h) * 0.5f);

            if (radius <= 0f || triNum <= 1)
            {
                // 角丸なし（矩形）
                points.Add(new Vector2(xMin, yMax));
                points.Add(new Vector2(xMax, yMax));
                points.Add(new Vector2(xMax, yMin));
                points.Add(new Vector2(xMin, yMin));
                return;
            }

            // コーナー中心
            Vector2 cTR = new Vector2(xMax - radius, yMax - radius);
            Vector2 cTL = new Vector2(xMin + radius, yMax - radius);
            Vector2 cBL = new Vector2(xMin + radius, yMin + radius);
            Vector2 cBR = new Vector2(xMax - radius, yMin + radius);

            // 右上（0 -> 90度）
            AppendArc(points, cTR, radius, 0f, Mathf.PI * 0.5f, triNum);
            // 左上（90 -> 180度）
            AppendArc(points, cTL, radius, Mathf.PI * 0.5f, Mathf.PI, triNum);
            // 左下（180 -> 270度）
            AppendArc(points, cBL, radius, Mathf.PI, Mathf.PI * 1.5f, triNum);
            // 右下（270 -> 360度）
            AppendArc(points, cBR, radius, Mathf.PI * 1.5f, Mathf.PI * 2f, triNum);

            // 最後の点は最初と同じになりがちなので、重複を除去
            if (points.Count >= 2)
            {
                var first = points[0];
                var last = points[points.Count - 1];
                if ((first - last).sqrMagnitude < 1e-6f)
                {
                    points.RemoveAt(points.Count - 1);
                }
            }
        }

        private static void AppendArc(List<Vector2> points, Vector2 center, float radius, float start, float end, int triNum)
        {
            // 端点込みで triNum 分割（= triNum+1 点）
            for (int i = 0; i <= triNum; i++)
            {
                float t = (float)i / triNum;
                float a = Mathf.Lerp(start, end, t);

                float x = center.x + Mathf.Cos(a) * radius;
                float y = center.y + Mathf.Sin(a) * radius;

                // 直前と同じ点は入れない（角の継ぎ目対策）
                if (points.Count > 0)
                {
                    var p = points[points.Count - 1];
                    if ((p.x - x) * (p.x - x) + (p.y - y) * (p.y - y) < 1e-6f)
                        continue;
                }

                points.Add(new Vector2(x, y));
            }
        }

        /// <summary>
        /// 指定コーナーの1/4円弧を頂点追加し、三角形扇で埋める（塗りつぶし用）。
        /// </summary>
        private static void BuildCornerArc(
            VertexHelper vh,
            Vector2 centerV,
            Vector2 centerUV,
            int centerVertIndex,
            float radius,
            float uvRadiusX,
            float uvRadiusY,
            float startAngle,
            float delta,
            int triNum,
            Color32 color32)
        {
            int baseVert = vh.currentVertCount;

            for (int i = 0; i <= triNum; i++)
            {
                float a = startAngle + delta * i;
                float cosA = Mathf.Cos(a);
                float sinA = Mathf.Sin(a);

                Vector3 pos = new Vector3(centerV.x + cosA * radius, centerV.y + sinA * radius);
                Vector2 uvPos = new Vector2(centerUV.x + cosA * uvRadiusX, centerUV.y + sinA * uvRadiusY);

                vh.AddVert(pos, color32, uvPos);
            }

            for (int i = 0; i < triNum; i++)
            {
                vh.AddTriangle(centerVertIndex, baseVert + i + 1, baseVert + i);
            }
        }

        private Vector4 GetDrawingDimensions(bool shouldPreserveAspect)
        {
            var padding = overrideSprite == null ? Vector4.zero : DataUtility.GetPadding(overrideSprite);
            Rect r = GetPixelAdjustedRect();

            var size = overrideSprite == null
                ? new Vector2(r.width, r.height)
                : new Vector2(overrideSprite.rect.width, overrideSprite.rect.height);

            int spriteW = Mathf.RoundToInt(size.x);
            int spriteH = Mathf.RoundToInt(size.y);

            if (shouldPreserveAspect && size.sqrMagnitude > 0.0f)
            {
                float spriteRatio = size.x / size.y;
                float rectRatio = r.width / r.height;

                if (spriteRatio > rectRatio)
                {
                    float oldHeight = r.height;
                    r.height = r.width * (1.0f / spriteRatio);
                    r.y += (oldHeight - r.height) * rectTransform.pivot.y;
                }
                else
                {
                    float oldWidth = r.width;
                    r.width = r.height * spriteRatio;
                    r.x += (oldWidth - r.width) * rectTransform.pivot.x;
                }
            }

            var vv = new Vector4(
                spriteW > 0 ? padding.x / spriteW : 0f,
                spriteH > 0 ? padding.y / spriteH : 0f,
                spriteW > 0 ? (spriteW - padding.z) / spriteW : 1f,
                spriteH > 0 ? (spriteH - padding.w) / spriteH : 1f
            );

            vv = new Vector4(
                r.x + r.width * vv.x,
                r.y + r.height * vv.y,
                r.x + r.width * vv.z,
                r.y + r.height * vv.w
            );

            return vv;
        }
        /// <summary>
        /// 角丸矩形の外周点を「固定点数」で生成する。
        /// 4コーナーそれぞれで (triNum+1) 点を必ず出すため、
        /// radius==0 でも外側と同じ点数を確保できる（= 枠が塗りつぶしにフォールバックしない）。
        /// </summary>
        private static void BuildRoundedRectPerimeterPointsFixed(List<Vector2> points, float xMin, float yMin, float xMax, float yMax, float radius, int triNum)
        {
            points.Clear();

            float w = xMax - xMin;
            float h = yMax - yMin;

            if (w <= 0f || h <= 0f)
                return;

            triNum = Mathf.Max(1, triNum);

            radius = Mathf.Clamp(radius, 0f, Mathf.Min(w, h) * 0.5f);

            // コーナー中心（radius==0 の場合は実質「角そのもの」になる）
            Vector2 cTR = new Vector2(xMax - radius, yMax - radius);
            Vector2 cTL = new Vector2(xMin + radius, yMax - radius);
            Vector2 cBL = new Vector2(xMin + radius, yMin + radius);
            Vector2 cBR = new Vector2(xMax - radius, yMin + radius);

            // 右上（0 -> 90度）
            AppendArcFixed(points, cTR, radius, 0f, Mathf.PI * 0.5f, triNum);
            // 左上（90 -> 180度）
            AppendArcFixed(points, cTL, radius, Mathf.PI * 0.5f, Mathf.PI, triNum);
            // 左下（180 -> 270度）
            AppendArcFixed(points, cBL, radius, Mathf.PI, Mathf.PI * 1.5f, triNum);
            // 右下（270 -> 360度）
            AppendArcFixed(points, cBR, radius, Mathf.PI * 1.5f, Mathf.PI * 2f, triNum);

            // Fixed 方式は「点数固定」が目的なので、重複除去はしない
            // （radius==0 のとき同一点が並ぶが、フォールバックせず枠が成立するのが優先）
        }

        private static void AppendArcFixed(List<Vector2> points, Vector2 center, float radius, float start, float end, int triNum)
        {
            for (int i = 0; i <= triNum; i++)
            {
                float t = (float)i / triNum;
                float a = Mathf.Lerp(start, end, t);

                float x = center.x + Mathf.Cos(a) * radius;
                float y = center.y + Mathf.Sin(a) * radius;

                points.Add(new Vector2(x, y));
            }
        }
    }
}
