using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace HikanyanLibrary.UITools.Editor
{
    /// <summary>
    /// SimpleRoundedImage 用のカスタムInspector。
    /// ImageEditor（標準の Image Inspector）を継承しつつ、
    /// 独自パラメータ（Shape / Radius / TriangleNum / Line / TriangleRotationDeg）を追加表示する。
    /// </summary>
    [CustomEditor(typeof(SimpleRoundedImage), true)]
    [CanEditMultipleObjects]
    public class SimpleRoundedImageEditor : ImageEditor
    {
        private SerializedProperty _shape;
        private SerializedProperty _radius;
        private SerializedProperty _triangleNum;
        private SerializedProperty _line;
        private SerializedProperty _triangleRotationDeg;

        protected override void OnEnable()
        {
            base.OnEnable();

            _shape = serializedObject.FindProperty("Shape");
            _radius = serializedObject.FindProperty("Radius");
            _triangleNum = serializedObject.FindProperty("TriangleNum");
            _line = serializedObject.FindProperty("Line");
            _triangleRotationDeg = serializedObject.FindProperty("TriangleRotationDeg");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ---- ImageEditor 標準 ----
            SpriteGUI();
            AppearanceControlsGUI();
            RaycastControlsGUI();

            bool showNativeSize = (target as Image)?.sprite != null;
            m_ShowNativeSize.target = showNativeSize;
            NativeSizeButtonGUI();
            // ---- ここまで ----

            EditorGUILayout.Space(8);

            // ---- SimpleRoundedImage 独自 ----
            EditorGUILayout.LabelField("Shape Settings", EditorStyles.boldLabel);

            if (_shape == null)
            {
                EditorGUILayout.HelpBox("Shape プロパティが見つかりません。SimpleRoundedImage 側のフィールド名を確認してください。", MessageType.Error);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.PropertyField(_shape, new GUIContent("Shape", "描画形状（角丸矩形 / 三角形）"));

            int shapeIndex = _shape.enumValueIndex; // RoundedRect=0, Triangle=1 の想定

            EditorGUILayout.Space(6);

            if (shapeIndex == 0)
            {
                // RoundedRect
                EditorGUILayout.LabelField("RoundedRect", EditorStyles.boldLabel);

                if (_radius != null)
                {
                    EditorGUILayout.PropertyField(_radius, new GUIContent("Radius", "角丸の半径（0以上）"));
                    if (_radius.propertyType == SerializedPropertyType.Float && _radius.floatValue < 0f)
                    {
                        EditorGUILayout.HelpBox("Radius は 0 以上を推奨します（実行時は 0 にクランプされます）。", MessageType.Warning);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Radius プロパティが見つかりません。", MessageType.Error);
                }

                if (_triangleNum != null)
                {
                    EditorGUILayout.PropertyField(_triangleNum, new GUIContent("Corner Triangle Num", "各コーナーの1/4円を近似する三角形数。大きいほど滑らかだが負荷増。"));

                    if (_triangleNum.propertyType == SerializedPropertyType.Integer && _triangleNum.intValue >= 12)
                    {
                        EditorGUILayout.HelpBox("Triangle Num が大きめです。UI頂点数が増え、Canvas負荷が上がる可能性があります。", MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("TriangleNum プロパティが見つかりません。", MessageType.Error);
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Border", EditorStyles.boldLabel);

                if (_line != null)
                {
                    EditorGUILayout.PropertyField(_line, new GUIContent("Line", "枠の太さ。0で塗りつぶし、0より大きいと内側をくり抜いた枠になります。"));

                    // 参考情報：Radius と Line の関係（今回の要件）
                    if (_line.propertyType == SerializedPropertyType.Float && _radius != null && _radius.propertyType == SerializedPropertyType.Float)
                    {
                        if (_line.floatValue > 0f && Mathf.Abs(_line.floatValue - _radius.floatValue) < 0.0001f)
                        {
                            EditorGUILayout.HelpBox("Line と Radius が同値の場合、内側角が 0 になりやすい設定です（枠のみ表示）。", MessageType.None);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Line プロパティが見つかりません。", MessageType.Error);
                }
            }
            else
            {
                // Triangle
                EditorGUILayout.LabelField("Triangle", EditorStyles.boldLabel);

                if (_triangleRotationDeg != null)
                {
                    EditorGUILayout.PropertyField(_triangleRotationDeg, new GUIContent("Rotation (Deg)", "三角形の回転角（度）。0で上向き頂点。"));
                }
                else
                {
                    EditorGUILayout.HelpBox("TriangleRotationDeg プロパティが見つかりません。", MessageType.Error);
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Border", EditorStyles.boldLabel);

                if (_line != null)
                {
                    EditorGUILayout.PropertyField(_line, new GUIContent("Line", "枠の太さ。0で塗りつぶし、0より大きいと内側をくり抜いた枠になります。"));
                }
                else
                {
                    EditorGUILayout.HelpBox("Line プロパティが見つかりません。", MessageType.Error);
                }

                EditorGUILayout.HelpBox("Triangle モードでは Radius / Corner Triangle Num は使用しません。", MessageType.None);
            }

            serializedObject.ApplyModifiedProperties();

            // 形状・頂点が変わるプロパティなので、確実に再メッシュ生成されるようにする
            if (GUI.changed)
            {
                foreach (var obj in targets)
                {
                    if (obj is SimpleRoundedImage img)
                    {
                        EditorUtility.SetDirty(img);
                        img.SetVerticesDirty();
                        img.SetMaterialDirty();
                    }
                }
            }
        }
    }
}
