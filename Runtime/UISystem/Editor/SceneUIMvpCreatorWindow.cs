#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace HikanyanLibrary.UISystem.Editor
{
    public sealed class SceneUIMvpCreatorWindow : EditorWindow
    {
        private enum Mode
        {
            Scene,
            Prefab
        }

        private static Mode s_openMode = Mode.Scene;

        // 既存の固定パスは「初回デフォルト候補」として残す（存在すればこれを使える）
        private const string DefaultSettingsAssetPath =
            "Assets/HikanyanLibrary/Scripts/UISystem/Editor/UIMvpCreatorSettings.asset";

        // Settings パスを保持（任意パス対応）
        private const string SettingsPathPrefsKey = "Hikanyan.UIMvpCreator.SettingsPath";
        private string _settingsPath = string.Empty;

        private UIMvpCreatorSettings _settings = null!;

        // UI state
        private string _uiName = "NewUI";
        private string _namespace = "HikanyanLaboratory.UI.UISystem";

        private bool _overwriteScripts;
        private bool _overwritePrefab;
        private bool _closeOnRegister = true;

        // UIElements containers
        private VisualElement _contentContainer = null!;

        [MenuItem("GameObject/Hikanyan/UI/Create Scene UI MVP...", false, 10)]
        private static void OpenScene()
        {
            s_openMode = Mode.Scene;
            OpenInternal();
        }

        [MenuItem("GameObject/Hikanyan/UI/Create Prefab UI MVP...", false, 10)]
        private static void OpenPrefab()
        {
            s_openMode = Mode.Prefab;
            OpenInternal();
        }

        private static void OpenInternal()
        {
            var w = GetWindow<SceneUIMvpCreatorWindow>();
            w.titleContent = new GUIContent(s_openMode == Mode.Scene ? "Create Scene UI MVP" : "Create Prefab UI MVP");
            w.minSize = new Vector2(720, 360);
            w.Show();
        }

        private void OnEnable()
        {
            _settings = LoadOrCreateSettings();

            // Settings -> UI state
            ApplySettingsToUiState();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            rootVisualElement.Add(scroll);

            _contentContainer = new VisualElement();
            _contentContainer.style.paddingLeft = 10;
            _contentContainer.style.paddingRight = 10;
            _contentContainer.style.paddingTop = 10;
            _contentContainer.style.paddingBottom = 10;
            scroll.Add(_contentContainer);

            // Title
            var title = new Label(s_openMode == Mode.Scene
                ? "Create Scene UI MVP (Param / View / Presenter + SceneUIRegistrar)"
                : "Create Prefab UI MVP (Param / View / Presenter + Prefab)");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 13;
            _contentContainer.Add(title);

            _contentContainer.Add(new VisualElement { style = { height = 10 } });

            _contentContainer.Add(new Label("Settings")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold }
            });

            var settingsField = new ObjectField("Settings Asset")
            {
                objectType = typeof(UIMvpCreatorSettings),
                allowSceneObjects = false,
                value = _settings
            };
            settingsField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is UIMvpCreatorSettings s)
                {
                    _settings = s;
                    _settingsPath = AssetDatabase.GetAssetPath(_settings);
                    SaveSettingsPath(_settingsPath);

                    ApplySettingsToUiState();
                    CreateGUI(); // UI再構築して反映
                }
                else
                {
                    // null 選択は許容しない（事故防止）。元に戻す。
                    settingsField.SetValueWithoutNotify(_settings);
                }
            });
            _contentContainer.Add(settingsField);

            var settingsButtonsRow = new VisualElement();
            settingsButtonsRow.style.flexDirection = FlexDirection.Row;

            var newSettingsBtn = new Button(() =>
            {
                CreateNewSettings();
                ApplySettingsToUiState();
                CreateGUI(); // UI再構築して反映
            })
            {
                text = "New Settings..."
            };

            var pingBtn = new Button(() =>
            {
                if (_settings != null)
                {
                    EditorGUIUtility.PingObject(_settings);
                    Selection.activeObject = _settings;
                }
            })
            {
                text = "Ping"
            };

            settingsButtonsRow.Add(newSettingsBtn);

            // gap の代わりにスペーサーを挟む
            var spacer = new VisualElement();
            spacer.style.width = 6;
            settingsButtonsRow.Add(spacer);

            settingsButtonsRow.Add(pingBtn);

            _contentContainer.Add(settingsButtonsRow);

            // ---- Roots (D&D) ----
            _contentContainer.Add(new Label("Roots (Drag & Drop Folder)")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold }
            });

            AddFolderField(
                label: "Generated Root",
                getPath: () => _settings.GeneratedRoot,
                setPath: v => _settings.GeneratedRoot = v);

            AddFolderField(
                label: "Template Root",
                getPath: () => _settings.TemplateRoot,
                setPath: v => _settings.TemplateRoot = v);

            AddFolderField(
                label: "Prefab Root",
                getPath: () => _settings.PrefabRoot,
                setPath: v => _settings.PrefabRoot = v);

            _contentContainer.Add(new VisualElement { style = { height = 8 } });

            // ---- Basic ----
            var uiNameField = new TextField("UI Name") { value = _uiName };
            uiNameField.RegisterValueChangedCallback(e => _uiName = e.newValue);
            _contentContainer.Add(uiNameField);

            var nsField = new TextField("Namespace") { value = _namespace };
            nsField.RegisterValueChangedCallback(e => _namespace = e.newValue);
            _contentContainer.Add(nsField);

            _contentContainer.Add(new VisualElement { style = { height = 8 } });

            // ---- Options ----
            var overwriteScriptsToggle = new Toggle("Overwrite Existing Scripts") { value = _overwriteScripts };
            overwriteScriptsToggle.RegisterValueChangedCallback(e =>
            {
                _overwriteScripts = e.newValue;
                _settings.OverwriteScriptsDefault = _overwriteScripts;
                MarkDirtyAndSave(_settings);
            });
            _contentContainer.Add(overwriteScriptsToggle);

            if (s_openMode == Mode.Prefab)
            {
                var overwritePrefabToggle = new Toggle("Overwrite Existing Prefab") { value = _overwritePrefab };
                overwritePrefabToggle.RegisterValueChangedCallback(e =>
                {
                    _overwritePrefab = e.newValue;
                    _settings.OverwritePrefabDefault = _overwritePrefab;
                    MarkDirtyAndSave(_settings);
                });
                _contentContainer.Add(overwritePrefabToggle);
            }
            else
            {
                var closeToggle = new Toggle("Close On Register (SceneUIRegistrar)") { value = _closeOnRegister };
                closeToggle.RegisterValueChangedCallback(e => _closeOnRegister = e.newValue);
                _contentContainer.Add(closeToggle);
            }

            _contentContainer.Add(new VisualElement { style = { height = 12 } });

            // ---- Create button ----
            var btn = new Button(() =>
            {
                if (s_openMode == Mode.Scene) CreateScene();
                else CreatePrefab();
            })
            {
                text = "Create"
            };
            btn.style.height = 30;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            _contentContainer.Add(btn);

            _contentContainer.Add(new VisualElement { style = { height = 10 } });

            var info = new HelpBox(
                "生成ルール:\n" +
                "- Scene scripts : <GeneratedRoot>/Scene/<UIName>/\n" +
                "- Prefab scripts: <GeneratedRoot>/Prefab/<UIName>/\n" +
                "- Templates     : <TemplateRoot>/Scene , <TemplateRoot>/Prefab\n" +
                "- Prefab asset  : <PrefabRoot>/<UIName>.prefab\n\n" +
                "テンプレートは Param.txt / View.txt / Presenter.txt が必須（フォールバック無し）。\n" +
                "Sceneは SceneUIRegistrar を付与。",
                HelpBoxMessageType.Info);
            _contentContainer.Add(info);
        }

        // ----------------------------
        // Settings <-> UI state
        // ----------------------------
        private void ApplySettingsToUiState()
        {
            _namespace = _settings.DefaultNamespace;
            _overwriteScripts = _settings.OverwriteScriptsDefault;
            _overwritePrefab = _settings.OverwritePrefabDefault;
        }

        // ----------------------------
        // D&D folder field
        // ----------------------------
        private void AddFolderField(string label, Func<string> getPath, Action<string> setPath)
        {
            var field = new ObjectField(label)
            {
                objectType = typeof(DefaultAsset),
                allowSceneObjects = false,
                value = AssetDatabase.LoadAssetAtPath<DefaultAsset>(getPath())
            };

            field.RegisterValueChangedCallback(evt =>
            {
                var selected = evt.newValue as DefaultAsset;
                if (selected == null) return;

                var path = AssetDatabase.GetAssetPath(selected);
                if (!AssetDatabase.IsValidFolder(path))
                {
                    Debug.LogWarning("フォルダ以外が選択されました");
                    field.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<DefaultAsset>(getPath()));
                    return;
                }

                setPath(path);
                MarkDirtyAndSave(_settings);
            });

            _contentContainer.Add(field);
        }

        private static void MarkDirtyAndSave(UnityEngine.Object obj)
        {
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }

        // ----------------------------
        // Settings load/create (任意パス対応)
        // ----------------------------
        private UIMvpCreatorSettings LoadOrCreateSettings()
        {
            // 1) EditorPrefs から復元
            _settingsPath = EditorPrefs.GetString(SettingsPathPrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(_settingsPath))
            {
                var s = AssetDatabase.LoadAssetAtPath<UIMvpCreatorSettings>(_settingsPath);
                if (s != null) return s;

                // 壊れたパスはクリア
                _settingsPath = string.Empty;
                EditorPrefs.DeleteKey(SettingsPathPrefsKey);
            }

            // 2) 既存のデフォルトパスがあればそれを使う（後方互換）
            var defaultS = AssetDatabase.LoadAssetAtPath<UIMvpCreatorSettings>(DefaultSettingsAssetPath);
            if (defaultS != null)
            {
                _settingsPath = DefaultSettingsAssetPath;
                SaveSettingsPath(_settingsPath);
                return defaultS;
            }

            // 3) なければデフォルトパスに作成（従来挙動）
            EnsureFolder(Path.GetDirectoryName(DefaultSettingsAssetPath)!.Replace("\\", "/"));
            var created = ScriptableObject.CreateInstance<UIMvpCreatorSettings>();
            AssetDatabase.CreateAsset(created, DefaultSettingsAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _settingsPath = DefaultSettingsAssetPath;
            SaveSettingsPath(_settingsPath);
            return created;
        }

        private void CreateNewSettings()
        {
            var newSettings = ScriptableObject.CreateInstance<UIMvpCreatorSettings>();

            string path = EditorUtility.SaveFilePanelInProject(
                "Save UI MVP Creator Settings",
                "UIMvpCreatorSettings",
                "asset",
                "保存する場所を選んでください");

            if (string.IsNullOrEmpty(path))
            {
                DestroyImmediate(newSettings);
                return;
            }

            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                directory = directory.Replace("\\", "/");
                if (!AssetDatabase.IsValidFolder(directory))
                {
                    EnsureFolder(directory);
                }
            }

            AssetDatabase.CreateAsset(newSettings, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _settings = newSettings;
            _settingsPath = path;
            SaveSettingsPath(_settingsPath);

            EditorGUIUtility.PingObject(_settings);
            Selection.activeObject = _settings;
        }

        private static void SaveSettingsPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                EditorPrefs.DeleteKey(SettingsPathPrefsKey);
            else
                EditorPrefs.SetString(SettingsPathPrefsKey, path);
        }

        // ----------------------------
        // Create Scene
        // ----------------------------
        private void CreateScene()
        {
            var baseName = SanitizeToIdentifier(_uiName);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                EditorUtility.DisplayDialog("Error", "有効なUI名を入力してください。", "OK");
                return;
            }

            var paramName = $"{baseName}Param";
            var viewName = $"{baseName}View";
            var presenterName = $"{baseName}Presenter";

            // EnsureFolder より前に衝突判定
            if (HasCrossModeCollision(s_openMode, _settings, baseName, paramName, viewName, presenterName,
                    out var collisionMsg))
            {
                EditorUtility.DisplayDialog("Name Collision", collisionMsg, "OK");
                return;
            }

            var scriptsDir = $"{_settings.GeneratedRoot}/Scene/{baseName}";
            EnsureFolder(scriptsDir);

            var paramPath = $"{scriptsDir}/{paramName}.cs";
            var viewPath = $"{scriptsDir}/{viewName}.cs";
            var presenterPath = $"{scriptsDir}/{presenterName}.cs";

            if (!ConfirmOverwriteIfNeeded(new[] { paramPath, viewPath, presenterPath }, _overwriteScripts))
                return;

            var templateDir = $"{_settings.TemplateRoot}/Scene";
            if (!TryGenerateFromTemplates(templateDir, _namespace, baseName, paramName, viewName, presenterName,
                    out var paramContent, out var viewContent, out var presenterContent, out var error))
            {
                EditorUtility.DisplayDialog("Template Error", error, "OK");
                return;
            }

            File.WriteAllText(paramPath, paramContent);
            File.WriteAllText(viewPath, viewContent);
            File.WriteAllText(presenterPath, presenterContent);
            AssetDatabase.Refresh();

            var parent = Selection.activeTransform;
            var root = new GameObject(baseName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(root, "Create Scene UI Root");
            if (parent != null) root.transform.SetParent(parent, worldPositionStays: false);

            var registrar = root.GetComponent<SceneUIRegistrar>() ?? Undo.AddComponent<SceneUIRegistrar>(root);
            SetPrivateBool(registrar, "_closeOnRegister", _closeOnRegister);

            ScenePendingStore.Save(new ScenePending
            {
                RootGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(root).ToString(),
                Namespace = _namespace,
                ViewName = viewName,
                PresenterName = presenterName,
            });

            UIMvpCreatorPostCompile.TryAttachNow();
        }

        // ----------------------------
        // Create Prefab
        // ----------------------------
        private void CreatePrefab()
        {
            var baseName = SanitizeToIdentifier(_uiName);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                EditorUtility.DisplayDialog("Error", "有効なUI名を入力してください。", "OK");
                return;
            }

            var paramName = $"{baseName}Param";
            var viewName = $"{baseName}View";
            var presenterName = $"{baseName}Presenter";

            // Prefab 側も同様に（EnsureFolder 前）
            if (HasCrossModeCollision(s_openMode, _settings, baseName, paramName, viewName, presenterName,
                    out var collisionMsg))
            {
                EditorUtility.DisplayDialog("Name Collision", collisionMsg, "OK");
                return;
            }

            var scriptsDir = $"{_settings.GeneratedRoot}/Prefab/{baseName}";
            EnsureFolder(scriptsDir);
            EnsureFolder(_settings.PrefabRoot);

            var paramPath = $"{scriptsDir}/{paramName}.cs";
            var viewPath = $"{scriptsDir}/{viewName}.cs";
            var presenterPath = $"{scriptsDir}/{presenterName}.cs";

            if (!ConfirmOverwriteIfNeeded(new[] { paramPath, viewPath, presenterPath }, _overwriteScripts))
                return;

            var prefabPath = $"{_settings.PrefabRoot}/{baseName}.prefab";
            if (!ConfirmOverwritePrefabIfNeeded(prefabPath, _overwritePrefab))
                return;

            var templateDir = $"{_settings.TemplateRoot}/Prefab";
            if (!TryGenerateFromTemplates(templateDir, _namespace, baseName, paramName, viewName, presenterName,
                    out var paramContent, out var viewContent, out var presenterContent, out var error))
            {
                EditorUtility.DisplayDialog("Template Error", error, "OK");
                return;
            }

            File.WriteAllText(paramPath, paramContent);
            File.WriteAllText(viewPath, viewContent);
            File.WriteAllText(presenterPath, presenterContent);
            AssetDatabase.Refresh();

            var tempRoot = new GameObject(baseName, typeof(RectTransform));
            try
            {
                var saved = PrefabUtility.SaveAsPrefabAsset(tempRoot, prefabPath);
                if (saved == null)
                {
                    EditorUtility.DisplayDialog("Error", "Prefab の保存に失敗しました。", "OK");
                    return;
                }
            }
            finally
            {
                DestroyImmediate(tempRoot);
            }

            PrefabPendingStore.Save(new PrefabPending
            {
                PrefabAssetPath = prefabPath,
                Namespace = _namespace,
                ViewName = viewName,
                PresenterName = presenterName,
            });

            UIMvpCreatorPostCompile.TryAttachNow();

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset != null)
            {
                Selection.activeObject = prefabAsset;
                EditorGUIUtility.PingObject(prefabAsset);
            }
        }

        // ----------------------------
        // Templates
        // ----------------------------
        private static bool TryGenerateFromTemplates(
            string templateFolder,
            string ns,
            string baseName,
            string paramName,
            string viewName,
            string presenterName,
            out string param,
            out string view,
            out string presenter,
            out string error)
        {
            param = view = presenter = string.Empty;
            error = string.Empty;

            var tokens = new Dictionary<string, string>
            {
                ["NAMESPACE"] = ns,
                ["BASE_NAME"] = baseName,
                ["PARAM_CLASS"] = paramName,
                ["VIEW_CLASS"] = viewName,
                ["PRESENTER_CLASS"] = presenterName,
                ["PRESENTER_BASE"] = $"PresenterBase<{viewName}, {paramName}>",
            };

            var paramTplPath = $"{templateFolder}/Param.txt";
            var viewTplPath = $"{templateFolder}/View.txt";
            var presenterTplPath = $"{templateFolder}/Presenter.txt";

            var missing = new List<string>();
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(paramTplPath) == null) missing.Add("Param.txt");
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(viewTplPath) == null) missing.Add("View.txt");
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(presenterTplPath) == null) missing.Add("Presenter.txt");

            if (missing.Count > 0)
            {
                error =
                    "テンプレートが見つかりません。\n\n" +
                    $"Template Folder: {templateFolder}\n" +
                    $"Missing: {string.Join(", ", missing)}";
                return false;
            }

            string e1 = "", e2 = "", e3 = "";
            var ok1 = TemplateRenderer.TryRender(paramTplPath, tokens, out param, out e1);
            var ok2 = TemplateRenderer.TryRender(viewTplPath, tokens, out view, out e2);
            var ok3 = TemplateRenderer.TryRender(presenterTplPath, tokens, out presenter, out e3);

            if (!(ok1 && ok2 && ok3))
            {
                error = string.Join("\n\n", new[] { e1, e2, e3 }.Where(x => !string.IsNullOrWhiteSpace(x)));
                return false;
            }

            return true;
        }

        // 衝突チェック
        private static bool HasCrossModeCollision(
            Mode currentMode,
            UIMvpCreatorSettings settings,
            string baseName,
            string paramName,
            string viewName,
            string presenterName,
            out string message)
        {
            message = string.Empty;

            // 反対側のフォルダだけ見る（自分側はこれから作るので見ない）
            var otherDir = currentMode == Mode.Scene
                ? $"{settings.GeneratedRoot}/Prefab/{baseName}"
                : $"{settings.GeneratedRoot}/Scene/{baseName}";

            if (AssetDatabase.IsValidFolder(otherDir))
            {
                message =
                    "同名UIが反対モード側に既に存在します。\n\n" +
                    $"- Existing: {otherDir}\n\n" +
                    "UI名を変更するか、既存のフォルダ/スクリプトを削除してください。";
                return true;
            }

            // 保険：プロジェクト全体で同名クラス（=同名ファイル）が存在するか
            bool ExistsScript(string className)
            {
                var guids = AssetDatabase.FindAssets($"{className} t:Script");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var file = Path.GetFileNameWithoutExtension(path);
                    if (string.Equals(file, className, StringComparison.Ordinal))
                        return true;
                }

                return false;
            }

            var hit = new List<string>();
            if (ExistsScript(paramName)) hit.Add($"{paramName}.cs");
            if (ExistsScript(viewName)) hit.Add($"{viewName}.cs");
            if (ExistsScript(presenterName)) hit.Add($"{presenterName}.cs");

            if (hit.Count > 0)
            {
                message =
                    "同名のクラス（スクリプトファイル）が既にプロジェクト内に存在します。\n\n" +
                    string.Join("\n", hit.Select(x => $"- {x}")) +
                    "\n\n" +
                    "Scene/Prefab を跨いで同名クラスは共存できません。\n" +
                    "UI名を変更するか、namespace 分離（Scene用/Prefab用）を導入してください。";
                return true;
            }

            return false;
        }

        private static class TemplateRenderer
        {
            public static bool TryRender(
                string templateAssetPath,
                IReadOnlyDictionary<string, string> tokens,
                out string result,
                out string error)
            {
                result = string.Empty;
                error = string.Empty;

                var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(templateAssetPath);
                if (ta == null)
                {
                    error = $"テンプレートが読み込めません: {templateAssetPath}";
                    return false;
                }

                var content = ta.text;
                foreach (var kv in tokens)
                {
                    content = content.Replace($"{{{{{kv.Key}}}}}", kv.Value);
                }

                result = content;
                return true;
            }
        }

        // ----------------------------
        // Post compile attach
        // ----------------------------
        [InitializeOnLoad]
        private static class UIMvpCreatorPostCompile
        {
            static UIMvpCreatorPostCompile()
            {
                CompilationPipeline.compilationFinished += _ => TryAttachNow();
                EditorApplication.delayCall += TryAttachNow;
            }

            public static void TryAttachNow()
            {
                TryAttachScene();
                TryAttachPrefab();
            }

            private static void TryAttachScene()
            {
                if (!ScenePendingStore.TryLoad(out var pending)) return;

                var root = GlobalIdToObject(pending.RootGlobalId) as GameObject;
                if (root == null) return;

                var viewType = FindType($"{pending.Namespace}.{pending.ViewName}");
                var presenterType = FindType($"{pending.Namespace}.{pending.PresenterName}");
                if (viewType == null || presenterType == null) return;

                if (root.GetComponent(viewType) == null)
                    Undo.AddComponent(root, viewType);

                var presenter = root.GetComponent(presenterType) ?? Undo.AddComponent(root, presenterType);

                var registrar = root.GetComponent<SceneUIRegistrar>();
                if (registrar != null && presenter is UINodeBase node)
                {
                    SetPrivateObjectRef(registrar, "_node", node);
                }

                ScenePendingStore.Clear();
            }

            private static void TryAttachPrefab()
            {
                if (!PrefabPendingStore.TryLoad(out var pending)) return;
                if (string.IsNullOrWhiteSpace(pending.PrefabAssetPath)) return;

                var viewType = FindType($"{pending.Namespace}.{pending.ViewName}");
                var presenterType = FindType($"{pending.Namespace}.{pending.PresenterName}");
                if (viewType == null || presenterType == null) return;

                var root = PrefabUtility.LoadPrefabContents(pending.PrefabAssetPath);
                try
                {
                    if (root.GetComponent(viewType) == null)
                        root.AddComponent(viewType);

                    if (root.GetComponent(presenterType) == null)
                        root.AddComponent(presenterType);

                    PrefabUtility.SaveAsPrefabAsset(root, pending.PrefabAssetPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                PrefabPendingStore.Clear();
                AssetDatabase.Refresh();
            }

            private static UnityEngine.Object? GlobalIdToObject(string globalId)
            {
                if (string.IsNullOrEmpty(globalId)) return null;
                if (!GlobalObjectId.TryParse(globalId, out var gid)) return null;
                return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            }

            private static Type? FindType(string fullName)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var t = asm.GetType(fullName, throwOnError: false);
                    if (t != null) return t;
                }

                return null;
            }

            private static void SetPrivateObjectRef(Component target, string fieldName, UnityEngine.Object? value)
            {
                var so = new SerializedObject(target);
                var sp = so.FindProperty(fieldName);
                if (sp == null) return;
                sp.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        [Serializable]
        private sealed class ScenePending
        {
            public string RootGlobalId = "";
            public string Namespace = "";
            public string ViewName = "";
            public string PresenterName = "";
        }

        [Serializable]
        private sealed class PrefabPending
        {
            public string PrefabAssetPath = "";
            public string Namespace = "";
            public string ViewName = "";
            public string PresenterName = "";
        }

        private static class ScenePendingStore
        {
            private const string Key = "Hikanyan.UIMvpCreator.ScenePending";
            public static void Save(ScenePending data) => EditorPrefs.SetString(Key, EditorJsonUtility.ToJson(data));

            public static bool TryLoad(out ScenePending data)
            {
                data = new ScenePending();
                if (!EditorPrefs.HasKey(Key)) return false;

                var json = EditorPrefs.GetString(Key);
                if (string.IsNullOrEmpty(json)) return false;

                EditorJsonUtility.FromJsonOverwrite(json, data);
                return !string.IsNullOrEmpty(data.RootGlobalId);
            }

            public static void Clear() => EditorPrefs.DeleteKey(Key);
        }

        private static class PrefabPendingStore
        {
            private const string Key = "Hikanyan.UIMvpCreator.PrefabPending";
            public static void Save(PrefabPending data) => EditorPrefs.SetString(Key, EditorJsonUtility.ToJson(data));

            public static bool TryLoad(out PrefabPending data)
            {
                data = new PrefabPending();
                if (!EditorPrefs.HasKey(Key)) return false;

                var json = EditorPrefs.GetString(Key);
                if (string.IsNullOrEmpty(json)) return false;

                EditorJsonUtility.FromJsonOverwrite(json, data);
                return !string.IsNullOrEmpty(data.PrefabAssetPath);
            }

            public static void Clear() => EditorPrefs.DeleteKey(Key);
        }

        // ----------------------------
        // Overwrite dialogs
        // ----------------------------
        private static bool ConfirmOverwriteIfNeeded(IEnumerable<string> paths, bool overwrite)
        {
            var existing = paths.Where(File.Exists).ToArray();
            if (existing.Length == 0) return true;

            if (!overwrite)
            {
                EditorUtility.DisplayDialog(
                    "Already Exists",
                    "以下のスクリプトが既に存在します。\n\n" +
                    string.Join("\n", existing.Select(p => $"- {Path.GetFileName(p)}")) +
                    "\n\n名前を変えるか、Overwrite Existing Scripts を有効にしてください。",
                    "OK");
                return false;
            }

            return EditorUtility.DisplayDialog(
                "Overwrite Warning",
                "Overwrite Existing Scripts が有効です。\n\n以下を上書きします：\n" +
                string.Join("\n", existing.Select(p => $"- {Path.GetFileName(p)}")) +
                "\n\n続行しますか？",
                "Overwrite",
                "Cancel");
        }

        private static bool ConfirmOverwritePrefabIfNeeded(string prefabPath, bool overwrite)
        {
            if (!File.Exists(prefabPath)) return true;

            if (!overwrite)
            {
                EditorUtility.DisplayDialog(
                    "Already Exists",
                    $"Prefab が既に存在します：\n{prefabPath}\n\nOverwrite Existing Prefab を有効にするか、名前を変えてください。",
                    "OK");
                return false;
            }

            return EditorUtility.DisplayDialog(
                "Overwrite Warning",
                "Overwrite Existing Prefab が有効です。\n\n以下のPrefabを上書きします：\n" +
                $"- {Path.GetFileName(prefabPath)}\n\n続行しますか？",
                "Overwrite",
                "Cancel");
        }

        // ----------------------------
        // Utils
        // ----------------------------
        private static string SanitizeToIdentifier(string raw)
        {
            var s = new string(raw.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (char.IsDigit(s[0])) s = "_" + s;
            return s;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;
            folderPath = folderPath.Replace("\\", "/");

            if (AssetDatabase.IsValidFolder(folderPath)) return;

            var parts = folderPath.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                throw new Exception("Folder は Assets から始まる必要があります。");

            var current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void SetPrivateBool(Component target, string fieldName, bool value)
        {
            var so = new SerializedObject(target);
            var sp = so.FindProperty(fieldName);
            if (sp == null) return;
            sp.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}