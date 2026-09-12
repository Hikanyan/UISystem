# Changelog

## 2.0.0-preview.1

- Open／Closeの状態管理、失敗時のrollback、Bind寿命、破棄時解除を追加。
- Addressablesのロード参照をインスタンスの寿命に結び付け、キャンセルを伝播。
- Scene登録の重複拒否・解除・unscaled timeoutとUIScopeを追加。
- UIKey、UICatalog、UIHandle、型付きダイアログ結果、入力遮断と履歴、KeepAliveを追加。
- UIBootstrap、Safe Area、フェード、StateStore／Bindingsを追加。
- 生成テンプレート、namespace検証、生成キー、初回導入を修正。
- 動作サンプル、ライフサイクル・ロード・生成テストとCIを追加。
- 最低Unityを6000.3とし、Core依存とルートの重複package.jsonを削除。
