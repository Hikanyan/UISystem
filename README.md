# Hikanyan UISystem

Unity 6.3 / uGUI向けのゲームUI基盤です。現在は **2.0.0-preview.1**。型付き画面キー、MVP、非同期開閉、Sceneスコープ、ダイアログ結果、モーダル、戻る操作、上限付きKeepAliveを提供します。

## 導入

リポジトリルートはUnityパッケージではありません。Package Managerではサブディレクトリを指定してください。

1. UniTaskを先に導入します。
   `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#a9e27c03d411d2fca01cc7410c24c97cd77cb539`
2. UISystemを導入します。
   `https://github.com/Hikanyan/UISystem.git?path=/Packages/com.hikanyan.uisystem`
3. Package ManagerのSamplesから **Minimal Inventory and Dialog** をImportし、`Minimal.unity`を開いて再生します。サンプルにはInput Systemが必要です。

ブランチを試す場合はUISystemのURL末尾へ `#codex/uisystem-foundation` を付けます。ブランチはGitサーバーへpushされて初めて利用できます。公開後は検証済みのタグまたはcommitに固定してください。この作業ではpush・公開していません。

AddressablesとuGUIはパッケージ依存として解決されます。HikanyanLibrary-Coreは不要です。検証対象はUnity **6000.3.15f1**です。

## パッケージ

| パッケージ | 用途 |
|---|---|
| `Packages/com.hikanyan.uisystem` | UI基盤・作成ツール・サンプル・テスト |
| `Packages/com.hikanyan.prefabkeysgenerator` | 任意のAddressablesキー生成。UISystem導入後に追加 |
| `Packages/com.hikanyan.uitools` | 任意のテーマ・ボタン・スクロール・描画部品。UniTaskとLitMotion 2.0.2を先に導入 |

UITools用LitMotion:
`https://github.com/AnnulusGames/LitMotion.git?path=src/LitMotion/Assets/LitMotion#0b4c588ee75a07198841d92aab653e6b39445089`

## 制作の流れ

1. `GameObject > Hikanyan > UI > Create UI Bootstrap`でManager、Catalog、EventSystem、Canvasとレイヤーを用意します。Pageレイヤーが選択され、その下へScene UIを作成できます。
2. `Create Prefab UI MVP...`でArgs（Param）・View・Presenterを生成し、ViewへUI参照を設定します。
3. `Create > UI > Screen Definition`でID、Prefab、Layer、Reuseを指定し、Catalogへ追加します。
4. CatalogのInspectorで検証し、`Generate GameUI.g.cs`で型付きキーを生成します。
5. `await UIManager.Instance.OpenAsync(GameUI.Inventory, args, token)`で開き、返されたhandleの`CloseAsync`で閉じます。

詳しい寿命・データ連携・API仕様は `Packages/com.hikanyan.uisystem/Documentation~/Guide.md`、旧版からの移行は`Migration-2.0.md`を参照してください。

## 検証

`Tools/Prepare-ValidationProject.ps1`で個人用Assetsを含まない検証プロジェクトを作成できます。Git依存はcommit固定です。`-UseCachedDependencies`はローカルPackageCacheを使用するオフライン検証用です。

```powershell
pwsh -File Tools/Prepare-ValidationProject.ps1
```

CIは`.github/workflows/unity-validation.yml`。Unityライセンス用のRepository Secrets設定が必要です。実装・検証結果と残る実機確認は`Docs/Implementation-Status.md`へ記録します。

MIT License
