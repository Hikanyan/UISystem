# Hikanyan UI Tools

Unity 6000.3 / uGUI向けの任意パッケージです。UniTask 2.5.10とLitMotion 2.0.2を先にGit URLで導入してください（リポジトリREADME参照）。

色テーマ、ボタン共通処理、セル再利用スクロール、角丸、グラデーション、テキストスクロールを提供します。旧AssetsのスクリプトGUIDを保持しています。独自asmdefから使う場合は`Hikanyan.UITools`を参照します。

- InfiniteScroll: IInfiniteScroll実装は初期化・再利用・RefreshItemsで通知されます。循環リスト用で、有限データ件数の制約や非同期セル画像ロードの寿命は利用側で管理してください。
- ButtonCommon: Button.onClickを確定操作の入口に統一。OnClickedのPointerEventDataはnullです。
- UITheme: SetColor／Invalidateで更新し、Binderへ通知します。Providerは単一のアクティブな共通テーマとして使用します。
- TextScroller: Disableで停止・状態復元、Enable／文字・寸法変更で再計算します。
- DotIndicator: 自身が生成したDotのみを再生成します。旧配置に識別情報がない場合、専用コンテナの旧Dotは移行時に手動で整理してください。
- ShaderのGrabPassはBuilt-in Render Pipeline向けです。URP等で同じ結果を保証しません。Halftoneとグラデーションのバッチ／頂点数は実画面で計測してください。

Testsを実行する場合はmanifestのtestablesへ`com.hikanyan.uitools`を追加します。
