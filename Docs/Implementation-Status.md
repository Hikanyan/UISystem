# UISystem 改善実装状況

更新日: 2026-09-12

## 到達点

Unity 6000.3向けの配布用プレビュー基盤を実装しました。UISystemとPrefabKeysGeneratorは2.0.0-preview.1、任意のUI部品集は0.1.0-preview.1です。正式公開、push、タグ作成は行っていません。

## 実装済み

| 領域 | 内容 |
|---|---|
| 開閉と寿命 | 状態管理、非同期操作の直列化、キャンセル、失敗時の後始末、外部Destroy対応、Addressables所有権管理 |
| 呼び出し | 型付きUIKey・引数、UIHandle、型付きダイアログ結果、旧APIの維持 |
| 画面構成 | Catalog・Definition、HUD/Page/Modal/Toast、モーダル入力制御、戻る操作、フォーカス復帰 |
| データ連携 | UIStateStore、Bindingsの購読解除、表示単位のBindingToken |
| 再利用 | 上限付きKeepAliveキャッシュ。汎用PoolやLRUではありません |
| 制作支援 | BootstrapによるCanvasとレイヤー構築、Sceneスコープ、MVP生成、Catalog検証・型付きキー生成 |
| キー生成 | 既存Addressablesアドレス保持、重複検出、識別子検証、プロジェクト別設定 |
| UI部品 | 任意パッケージ化、スクロール再利用時の再バインド、Submit対応、テーマ更新、描画資源の解放 |
| 配布準備 | パッケージ定義、README、変更履歴、ライセンス、導入・移行ガイド、操作可能なMinimalサンプル |
| 検証環境 | 個人用Assetsを含まない検証プロジェクト生成スクリプト、テスト・Playerビルド用CI定義 |

## 検証結果

Unity 6000.3.15f1で、隔離した検証プロジェクトに配布対象3パッケージとMinimalサンプルをコピーして確認しました。第三者依存はローカルPackageCacheを使用しており、新規のリモートGit導入を検証したものではありません。

- EditMode: 10件成功、失敗0件（生成処理・Bootstrap）。
- PlayMode: 24件成功、失敗0件（寿命・画面操作・Addressables・UI部品・サンプルなど）。
- 性能測定テストを別途1件再実行し成功。これは上記24件に含まれるテストです。
- Windows Development Player: 最新の実装修正を含む最終ビルド成功（終了コード0）。画面を操作する実機QAは未実施です。

### 性能測定の範囲

空のPresenterをKeepAliveでウォームアップ後、100回開閉したEditor PlayMode測定は合計7.371ms、キャッシュ数1でした。描画を含まない単発の参考値であり、実機のフレーム時間や大量UIの性能保証には使えません。

このランタイムでは割り当て量カウンターが校正用の確保でも増えなかったため、ManagedAllocatedBytesは測定不能です。ゼロアロケーションとは評価していません。

ローカルの証跡は `Temp/validation-editmode-final.xml`、`Temp/validation-playmode-final.xml`、`Temp/validation-benchmark.xml`、`Temp/validation-build-final.log`、`Temp/PackageValidation/TestResults/UISystem-WarmOpenClose.txt` です。Tempは永続保存を保証しないため、主要結果を本書に記録しています。

## コミットの区切り

1. `5839d0a`: 状態遷移・リソース寿命と設計レビュー。
2. `add8441`: UI生成の安全性とAddressablesキー保持。
3. `4044e6b`: 型付きAPI・Catalog・ダイアログ・ナビゲーション・再利用。
4. `8fd68c1`: UI部品のパッケージ化と入力・バインド修正。
5. `93c9c9f`: Bootstrap制作導線、購読寿命、Addressables回帰テスト、Minimalサンプル。
6. 配布準備: パッケージ定義、ガイド、検証スクリプト、CI、本書。

## 使用開始と移行

導入はルートREADME、使い方は `Packages/com.hikanyan.uisystem/Documentation~/Guide.md`、破壊的変更は同ディレクトリの `Migration-2.0.md` を参照してください。

特に、UIManagerの暗黙生成廃止、最低Unityバージョン変更、Closeキャンセル時の終了処理、同型Scene UIの重複拒否、UIToolsのasmdef参照、ButtonCommonのイベント引数変更に注意が必要です。

## 正式配布までに残る確認

- リポジトリ公開後のGit URLからの新規導入と、依存解決の確認。
- Unityライセンス用Secretsの設定とリモートCI実行。CI定義は作成済みですが未実行です。
- 対象端末での描画、入力、解像度・SafeArea、実際の画面数とデータ量での性能測定。
- プレビュー利用からのAPI・制作手順のフィードバックと公開バージョン固定。

有限データの仮想リスト、汎用Pool、LRU、UI Toolkit対応は今回の実装範囲に含まれません。基盤のプレビュー実装完了と正式リリース完了は区別します。
