using System;
using System.IO;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

// あなたの UITheme クラス（ScriptableObject）が Assembly-CSharp に存在している前提
// UITheme.asset / UIThemeProjectSekai.preset からも UITheme が存在することは確認できています。
namespace HikanyanLibrary.UITools.Editor
{
    public class UIThemePresetManagerWindow : EditorWindow
    {
        [SerializeField] private UIThemePresetLibrary library;
        [SerializeField] private UITheme targetTheme;

        [SerializeField] private int selectedCategoryIndex = -1;

        private Vector2 leftScroll;
        private Vector2 rightScroll;

        private string newCategoryName = "Window";
        private string search = "";

        // プレビュー用
        private UITheme previewThemeInstance;
        private Preset previewPreset;

        [MenuItem("Tools/UI Theme/Theme Preset Manager")]
        public static void Open()
        {
            GetWindow<UIThemePresetManagerWindow>("UI Theme Presets");
        }

        private void OnEnable()
        {
            // 自動でライブラリを探す（プロジェクト内に1つある想定）
            if (library == null)
            {
                var guids = AssetDatabase.FindAssets("t:UIThemePresetLibrary");
                if (guids != null && guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    library = AssetDatabase.LoadAssetAtPath<UIThemePresetLibrary>(path);
                }
            }

            if (previewThemeInstance == null)
            {
                previewThemeInstance = ScriptableObject.CreateInstance<UITheme>();
                previewThemeInstance.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private void OnDisable()
        {
            if (previewThemeInstance != null)
            {
                DestroyImmediate(previewThemeInstance);
                previewThemeInstance = null;
            }
        }

        private void OnGUI()
        {
            DrawHeader();

            if (library == null)
            {
                EditorGUILayout.HelpBox("UIThemePresetLibrary が未設定です。Create Library で作成してください。", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLeftPane();
                DrawRightPane();
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    library = (UIThemePresetLibrary)EditorGUILayout.ObjectField("Library", library, typeof(UIThemePresetLibrary), false);

                    if (GUILayout.Button("Create Library", GUILayout.Width(120)))
                    {
                        CreateLibraryAsset();
                    }

                    if (library != null && GUILayout.Button("Ping", GUILayout.Width(60)))
                    {
                        EditorGUIUtility.PingObject(library);
                        Selection.activeObject = library;
                    }
                }

                targetTheme = (UITheme)EditorGUILayout.ObjectField("Target UITheme", targetTheme, typeof(UITheme), false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    newCategoryName = EditorGUILayout.TextField("New Category", newCategoryName);
                    if (GUILayout.Button("Add Category", GUILayout.Width(110)))
                    {
                        Undo.RecordObject(library, "Add UI Theme Category");
                        library.GetOrCreate(newCategoryName);
                        EditorUtility.SetDirty(library);
                    }
                }

                search = EditorGUILayout.TextField("Search Presets", search);
            }
        }

        private void DrawLeftPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(240)))
            {
                EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);

                leftScroll = EditorGUILayout.BeginScrollView(leftScroll, GUI.skin.box);

                for (int i = 0; i < library.categories.Count; i++)
                {
                    var cat = library.categories[i];
                    if (cat == null) continue;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var isSelected = (i == selectedCategoryIndex);

                        if (GUILayout.Toggle(isSelected, cat.name, "Button"))
                        {
                            selectedCategoryIndex = i;
                        }

                        if (GUILayout.Button("⋯", GUILayout.Width(28)))
                        {
                            ShowCategoryMenu(i);
                        }
                    }
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(6);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

                    GUI.enabled = targetTheme != null && selectedCategoryIndex >= 0;
                    if (GUILayout.Button("Create Preset From Target Theme"))
                    {
                        CreatePresetFromTargetTheme();
                    }
                    GUI.enabled = true;

                    GUI.enabled = selectedCategoryIndex >= 0;
                    if (GUILayout.Button("Add Existing Preset To Category"))
                    {
                        AddExistingPresetToCategory();
                    }
                    GUI.enabled = true;
                }
            }
        }

        private void DrawRightPane()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

                if (selectedCategoryIndex < 0 || selectedCategoryIndex >= library.categories.Count)
                {
                    EditorGUILayout.HelpBox("左側からカテゴリを選択してください。", MessageType.Info);
                    return;
                }

                var cat = library.categories[selectedCategoryIndex];
                if (cat == null)
                {
                    EditorGUILayout.HelpBox("カテゴリ参照が null です。", MessageType.Warning);
                    return;
                }

                rightScroll = EditorGUILayout.BeginScrollView(rightScroll, GUI.skin.box);

                for (int i = 0; i < cat.presets.Count; i++)
                {
                    var preset = cat.presets[i];
                    if (preset == null) continue;

                    if (!string.IsNullOrWhiteSpace(search) && !preset.name.Contains(search, StringComparison.OrdinalIgnoreCase))
                        continue;

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField(preset, typeof(Preset), false);

                            GUI.enabled = targetTheme != null;
                            if (GUILayout.Button("Apply", GUILayout.Width(60)))
                            {
                                ApplyPresetToTarget(preset);
                            }
                            GUI.enabled = targetTheme != null;

                            if (GUILayout.Button("Update", GUILayout.Width(60)))
                            {
                                // 現在のターゲットThemeの状態で Preset を上書き更新
                                UpdatePresetFromTarget(preset);
                            }
                            GUI.enabled = true;

                            if (GUILayout.Button("Duplicate", GUILayout.Width(80)))
                            {
                                DuplicatePresetAsset(preset);
                            }

                            if (GUILayout.Button("Ping", GUILayout.Width(50)))
                            {
                                EditorGUIUtility.PingObject(preset);
                                Selection.activeObject = preset;
                            }

                            if (GUILayout.Button("X", GUILayout.Width(24)))
                            {
                                RemovePresetReference(cat, preset);
                                break;
                            }
                        }

                        DrawPresetPreview(preset);
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawPresetPreview(Preset preset)
        {
            // UITheme の役割色一覧を簡易プレビュー
            // Presetは直接読めないので、一時インスタンスへ Apply して entries を表示する。
            if (previewThemeInstance == null) return;

            if (previewPreset != preset)
            {
                previewPreset = preset;
                // いったんクリーンにする：新しいインスタンスを作り直すのが安全
                DestroyImmediate(previewThemeInstance);
                previewThemeInstance = ScriptableObject.CreateInstance<UITheme>();
                previewThemeInstance.hideFlags = HideFlags.HideAndDontSave;
                preset.ApplyTo(previewThemeInstance);
            }

            // UITheme の entries を SerializedObject 経由で汎用描画（role + color）
            using (var so = new SerializedObject(previewThemeInstance))
            {
                var entries = so.FindProperty("entries");
                if (entries == null || !entries.isArray)
                {
                    EditorGUILayout.LabelField("Preview: entries が見つかりません。UIThemeのフィールド名が異なる可能性があります。");
                    return;
                }

                EditorGUILayout.LabelField("Preview", EditorStyles.miniBoldLabel);

                for (int i = 0; i < entries.arraySize; i++)
                {
                    var e = entries.GetArrayElementAtIndex(i);
                    var role = e.FindPropertyRelative("role");
                    var color = e.FindPropertyRelative("color");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(role, GUIContent.none, GUILayout.Width(140));
                        EditorGUILayout.PropertyField(color, GUIContent.none);

                        // 色チップ
                        var r = GUILayoutUtility.GetRect(24, 16, GUILayout.ExpandWidth(false));
                        EditorGUI.DrawRect(r, color.colorValue);
                    }
                }
            }
        }

        private void ShowCategoryMenu(int index)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Rename"), false, () =>
            {
                var cat = library.categories[index];
                var newName = EditorUtility.DisplayDialogComplex("Rename", "カテゴリ名を変更しますか？\n（簡易：Inspectorで編集推奨）", "OK", "Cancel", "") == 0;

                if (newName)
                {
                    // ダイアログ入力が無いので、ここは Inspector 編集を推奨
                    EditorGUIUtility.PingObject(library);
                    Selection.activeObject = library;
                }
            });

            menu.AddItem(new GUIContent("Delete Category"), false, () =>
            {
                if (!EditorUtility.DisplayDialog("Delete Category", "カテゴリを削除します（プリセットアセット自体は削除しません）。よろしいですか？", "Delete", "Cancel"))
                    return;

                Undo.RecordObject(library, "Delete UI Theme Category");
                library.categories.RemoveAt(index);
                EditorUtility.SetDirty(library);

                if (selectedCategoryIndex == index) selectedCategoryIndex = -1;
            });

            menu.ShowAsContext();
        }

        private void CreateLibraryAsset()
        {
            var folder = "Assets/Config";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "Config");
            }

            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/UIThemePresetLibrary.asset");
            var asset = ScriptableObject.CreateInstance<UIThemePresetLibrary>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            library = asset;
            EditorGUIUtility.PingObject(library);
            Selection.activeObject = library;
        }

        private void CreatePresetFromTargetTheme()
        {
            if (library == null || targetTheme == null) return;

            var cat = library.categories[selectedCategoryIndex];
            if (cat == null) return;

            var defaultName = $"{cat.name}_{targetTheme.name}";
            var savePath = EditorUtility.SaveFilePanelInProject(
                "Save UITheme Preset",
                defaultName,
                "preset",
                "保存先を選択してください。"
            );

            if (string.IsNullOrWhiteSpace(savePath)) return;

            var preset = new Preset(targetTheme);
            AssetDatabase.CreateAsset(preset, savePath);
            AssetDatabase.SaveAssets();

            Undo.RecordObject(library, "Add Preset To Category");
            cat.presets.Add(preset);
            EditorUtility.SetDirty(library);

            EditorGUIUtility.PingObject(preset);
            Selection.activeObject = preset;
        }

        private void AddExistingPresetToCategory()
        {
            if (library == null) return;
            var cat = library.categories[selectedCategoryIndex];
            if (cat == null) return;

            // Preset を1つ選んで追加（複数追加をやるならドラッグ&ドロップ拡張）
            var preset = Selection.activeObject as Preset;
            if (preset == null)
            {
                EditorUtility.DisplayDialog("Add Preset", "Projectウィンドウで追加したい .preset を選択してから実行してください。", "OK");
                return;
            }

            Undo.RecordObject(library, "Add Existing Preset");
            if (!cat.presets.Contains(preset))
            {
                cat.presets.Add(preset);
                EditorUtility.SetDirty(library);
            }
        }

        private void ApplyPresetToTarget(Preset preset)
        {
            if (targetTheme == null || preset == null) return;

            Undo.RecordObject(targetTheme, "Apply UI Theme Preset");
            preset.ApplyTo(targetTheme);
            EditorUtility.SetDirty(targetTheme);
            AssetDatabase.SaveAssets();

            // Scene上の UIThemeProvider を更新したい場合は、ここに任意で連携処理を入れる
            // （あなたのプロジェクトで UIThemeProvider を使っている場合のみ）
        }

        private void UpdatePresetFromTarget(Preset preset)
        {
            if (targetTheme == null || preset == null) return;

            // Preset.Update は「Preset内容を targetTheme の現在値で上書き」する
            Undo.RecordObject(preset, "Update UI Theme Preset");
            preset.UpdateProperties(targetTheme);
            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
        }

        private void DuplicatePresetAsset(Preset preset)
        {
            if (preset == null) return;

            var path = AssetDatabase.GetAssetPath(preset);
            if (string.IsNullOrWhiteSpace(path)) return;

            var dir = Path.GetDirectoryName(path)?.Replace("\\", "/") ?? "Assets";
            var newPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{preset.name}_Copy.preset");

            AssetDatabase.CopyAsset(path, newPath);
            AssetDatabase.SaveAssets();

            var copied = AssetDatabase.LoadAssetAtPath<Preset>(newPath);
            if (copied == null) return;

            var cat = library.categories[selectedCategoryIndex];
            Undo.RecordObject(library, "Add Duplicated Preset");
            cat.presets.Add(copied);
            EditorUtility.SetDirty(library);

            EditorGUIUtility.PingObject(copied);
            Selection.activeObject = copied;
        }

        private void RemovePresetReference(UIThemePresetLibrary.Category cat, Preset preset)
        {
            Undo.RecordObject(library, "Remove Preset Reference");
            cat.presets.Remove(preset);
            EditorUtility.SetDirty(library);
        }
    }
}
