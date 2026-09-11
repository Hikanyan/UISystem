# UISystem 設計・コードレビューと配布に向けた評価

評価日: 2026-09-12  
対象: 現在のワークスペースのソースコード、UPM構成、Editor生成ツール、サンプル、周辺UI部品  
目的: 複数のゲームで再利用でき、使用者が直感的に画面を量産できるUI基盤への発展

## 1. 総合評価

**MVPによる画面分離とScene／Prefabのライフタイム分離は、育てる価値のある土台です。ただし現状は、個人開発向けの試作基盤としての段階で、第三者に安定版として配布するには早いと評価します。**

優先すべきことは、画面の種類を増やすことより、失敗・キャンセル・連打・シーン破棄が起きても管理状態とリソースが整合する仕組みを作ることです。その上で、画面定義と型付きAPI、初回セットアップ、動作するサンプルを整えると、使いやすさと量産性が大きく改善します。

| 評価軸 | 現状 | 判断理由 |
|---|---|---|
| 基本設計 | 良い出発点 | Presenter／Viewの役割と生成／常駐の寿命が分離されている |
| 実行時の堅牢性 | 配布前に要修正 | 開閉競合、失敗時の巻き戻し、購読解除、ロード解放に穴がある |
| 使用APIの直感性 | 改善余地が大きい | 型2つ、文字列キー、整数IDを使用者が整合させる必要がある |
| データ管理 | 引数の受け渡しまで | 永続データ、表示状態、画面定義、結果返却の責務が未定義 |
| 画面の量産性 | 方針は良いが導入に障害 | テンプレート生成はあるが初期パス・namespace・再生成に問題がある |
| パフォーマンス | 計測前に解消すべき問題あり | Addressables参照残留、生成破棄、セル再利用時の割り当てなど |
| ゲームUIとしての機能 | 最小限 | 履歴、モーダル、フォーカス、入力遮断、画面スコープは未実装 |
| 配布準備 | 未完了 | 導入手順の不整合、依存関係、互換性、サンプル導線、検証基盤が不足 |

この評価は点数による客観測定ではなく、ソースに基づく定性評価です。FPS、CPU時間、メモリ増加量は測定していません。

## 2. 調査範囲と制約

- `Packages/com.hikanyan.uisystem`: Runtime、Editor、テンプレート、Samples~、package.json、asmdefを確認。
- `Packages/com.hikanyan.prefabkeysgenerator`: キー生成、Addressables自動登録、設定、別実装のローダーを確認。
- `Assets/HikanyanLibrary/UITools`: Button、Theme、Scroll、Text、Material制御を中心に確認。画像メッシュ処理とテーマEditorは構造・関連箇所の確認であり、全描画分岐の正しさを保証する監査ではありません。
- `README.md`、ルートpackage.json、ローカルmanifest、ProjectVersion、git追跡状態を確認。
- 依存CoreのローカルPackageCacheにあるSingleton実装も確認。外部依存の将来バージョンについては保証しません。
- Unity Editorでのコンパイル、PlayMode、Playerビルド、新規プロジェクトへのインストール、Profiler計測は未実施。以下の不具合は静的なコード追跡に基づき、再現条件と検証案を記載しています。
- 現在のローカルUnityは `6000.3.15f1`。パッケージの宣言値 `2020.3` とは別です。
- 周辺の`Assets/HikanyanLibrary/`などは調査時点で未追跡です。ローカルに存在する機能と、Git経由の配布物に入る機能を区別する必要があります。

本レビューでは実装コード・既存設定を変更していません。参照位置は評価時点の行番号です。以降、`UI/`は`Packages/com.hikanyan.uisystem/`、`Keys/`は`Packages/com.hikanyan.prefabkeysgenerator/`、`Widgets/`は`Assets/HikanyanLibrary/UITools/`を表します。

## 3. 現在の構成

```text
ゲーム側の呼び出し
  └─ UIManager（永続Singleton）
      ├─ Prefab: AddressablePrefabLoader → Instantiate → 登録 → Open
      │                                         └─ Close → Destroy
      └─ Scene: SceneUIRegistrar → 型／InstanceIDで登録 → Open／Close
          └─ UINodeBase（active制御・IsOpen）
              └─ PresenterBase<TView,TModel>（Bind／Unbind）
                  ├─ View（Unity Component）
                  └─ Parameter派生（画面へ渡すデータ）

制作支援: MVPコード生成 + Prefab生成 + Addressablesキー生成
周辺部品: テーマ、ボタン、スクロール、角丸、グラデーション、フェード
```

`Parameter`は実態として「画面を開くときの引数」です。MVPのModelという名前でも、ゲーム全体の状態管理や保存処理が実装されているわけではありません。

## 4. 良い部分と維持したい設計

1. **表示の管理と画面固有の処理を分けている。** `UIManager`、`UINodeBase`、`PresenterBase`に分かれており、画面ごとに登録・非表示処理を複製せずに済みます。
2. **Scene常駐と生成UIの寿命を区別している。** HUDと一時的なダイアログを同じ開閉概念で扱いながら、破棄の有無を分けられます。
3. **非同期をAPIの前提としている。** UniTaskとCancellationTokenを採用済みなので、ロードやアニメーションに発展させやすい構造です。ただし現在は後始末の保証が不足します。
4. **PresenterにView探索と型チェックを集約している。** 利用画面側の反復作業を減らしています。将来は型不整合をコンパイル時に検出できるAPIへ進められます。
5. **生成ツールとテンプレートが存在する。** 画面作成手順を統一する方向性は、量産に非常に有効です。設定アセットによるパス変更、Undoを使う処理もあります。
6. **Runtime／Editorのasmdefを分けている。** 配布とビルド依存の整理に適しています。
7. **テーマをScriptableObject化している。** 色を役割で参照する仕組みは、見た目の一括変更とバリエーション制作につながります。
8. **InfiniteScrollは一定数のセルを再利用する方針。** 全データ件数分のGameObjectを作らない点は良好です。ただしデータ更新の欠落と割り当ては修正が必要です。

## 5. コードレビュー: 優先修正項目

優先度は、P1＝安定配布前に修正、P2＝公開ベータまでに改善、P3＝段階的な整備です。「確認」はコード経路の確認を指し、Unityでの再現完了を意味しません。

### R01 [P1] Addressablesのロード所有権が失われ、解放できない

**根拠:** `UI/Runtime/AddressablePrefabLoader.cs:53–73,83–103`、`UI/Runtime/UIManager.cs:139–146`。`Keys/Runtime/AddressablePrefabLoader.cs`にも同じ所有権の問題があります。

- `LoadAssetAsync`のhandleをローカル変数に保持するだけで、成功後に返却・保管していません。Releaseはコメントアウトされています。
- Closeで行う`Destroy`は、通常の`Object.Instantiate`で作った複製を破棄するだけで、LoadAssetAsyncの参照を解放しません。
- 開閉の反復で未解放のロード参照が積み重なります。同じアセットが毎回丸ごと複製ロードされるという意味ではありませんが、不要になった依存アセットの解放を妨げます。

**改善:** `InstantiateAsync`と`ReleaseInstance`の対応、またはロードhandleを所有するleaseと通常Instantiate／Destroyの対応を統一する。成功・失敗・キャンセル・必要Component欠落の各経路で一度だけ解放する。単にfinallyのReleaseを復活させると、表示中のインスタンスが依存するアセットまで早期解放するため不十分です。

**検証:** 同一UIの100回開閉後、キャッシュ方針に応じた基準値まで参照数が戻ること。未登録キー・不正Prefab・ロード中キャンセルでも残留しないこと。

### R02 [P1] Open失敗後のノード・生成物・購読が残る

**根拠:** `UI/Runtime/UIManager.cs:124–127`、`UINodeBase.cs:41–49`、`PresenterBase.cs:34–46`。

登録してからOpenをawaitし、例外時に登録削除・破棄する処理がありません。型違いのParameter、OnBindの例外、開く演出のキャンセルで、呼び出し側にはIDが返らない一方、生成物と登録だけが残り得ます。IsOpenはfalseのため通常のClose処理もOnUnbindまで進みません。

さらにローダーの`GetComponent<T>()`がnullでも、すでに作ったGameObjectは破棄されません。

**改善:** 生成からOpen完了までを一つの操作として扱い、失敗時に登録・Bind・インスタンス・ロード所有権を巻き戻す。Bind途中の失敗にも耐える、冪等なcleanupを用意する。

### R03 [P1] IsOpenだけでは開閉の競合を扱えない

**根拠:** `UI/Runtime/UINodeBase.cs:41–60`、`UIManager.cs:133–146`。

Open中はIsOpen=falseなので、同じScene UIへOpenを2回呼ぶとBind／開く演出が重複します。Open途中のCloseは処理をせず戻り、その後Openが完了して表示が残ります。Close中はIsOpen=trueなのでCloseも多重実行され、生成UIでは破棄の競合にもつながります。

**改善:** `Closed / Opening / Open / Closing / Disposed`の状態と、進行中操作を管理する。推奨初期仕様は同一ノード内の直列化、同じOpen要求は進行中操作の完了を共有、Close要求は開く操作の中断と後始末まで待機すること。共有待機にはUniTaskの多重await制約を考慮した実装が必要です。

**検証:** Open連打、Close連打、Open直後Close、Close中Open、各時点のDestroyを組み合わせ、最終状態・Bind回数・解放回数を確認する。

### R04 [P1] キャンセルが失敗へ変換され、終了時の解除も保証されない

**根拠:** `UI/Runtime/AddressablePrefabLoader.cs:62–66,92–96`、`PresenterBase.cs:43–46`。

ローダーはOperationCanceledExceptionも一般例外として捕捉しnullを返すため、Managerはキャンセルを通常のExceptionへ変えます。またClose演出がキャンセル・失敗するとOnUnbindに到達しません。外部Destroy時の購読解除も基底クラスにはありません。

**改善:** キャンセルをキャンセルとして伝播する。呼び出し側tokenに加え、ノード／Managerの破棄に結び付く寿命tokenを使う。演出中断と最終cleanupを区別し、解除処理を既にキャンセルされたtokenに依存させない。通常Closeを中断して開いたままにする仕様なら、強制終了用のcleanup経路を別途保証する。

### R05 [P1] Scene登録の寿命・初期状態が不整合

**根拠:** `UI/Runtime/SceneUIRegistrar.cs:33–54`、`UIManager.cs:36–96`、`UINodeBase.cs:35–38`。

- `UnregisterSceneNode`はありますが、Registrarから破棄時に呼ばれません。永続Managerにはシーン破棄後の古い登録が残ります。一部は次回Open／登録時に削除されるだけです。
- `closeOnRegister=false`の場合、表示中のGameObjectでもIsOpen=falseのままになり、最初のCloseが何もしません。
- 同一型を複数登録すると、新ノードを`_nodes`へ追加してからエラーを出し、型検索は旧ノードを指したままです。Additive Sceneや同一画面の複数配置に不向きです。
- 非アクティブなルート自身にRegistrarを配置するとAwakeで登録する導線に到達せず、型Openの待機が解決しない構成になります。

**改善:** Sceneスコープで登録と解除を所有する。重複拒否は登録前に行う。初期表示も正規の初期化／Bindを通すか、常に初期Closedとする。非表示ノードの登録はアクティブなBootstrapから可能にする。単純にOnDisableで解除すると通常Close時も消えるため、破棄・スコープ終了と区別する。

### R06 [P2] 登録待機のタイムアウトがポーズ中に進まず、負けた待機が残る

**根拠:** `UI/Runtime/UIManager.cs:169–185`。

`UniTask.Delay(timeoutMs)`は既定で時間スケールの影響を受けます。`Time.timeScale=0`で未登録画面を開くと、想定した実時間5秒のタイムアウトになりません。WhenAnyは負けた操作を自動キャンセルしないので、タイムアウト後もWaitUntilが残り得ます。

**改善:** 登録完了通知をawaitする設計にし、実時間の期限と専用CTSで残った待機を止める。未登録・設定不備を即座に診断できるAPIも用意する。

### R07 [P1] 初期テンプレート設定では生成できず、任意namespaceにも対応不足

**根拠:** `UI/Editor/UIMvpCreatorSettings.cs:9–11`、`SceneUIMvpCreatorWindow.cs:308–340,550–596`、`UI/Editor/Template/*/Param.txt`と`Presenter.txt`。

初期TemplateRootはAssets内ですが、同梱テンプレートはPackages内です。初回に作る設定ではテンプレートを発見できません。同梱設定assetも別のAssetsパスを参照しています。またテンプレートはParameter、PresenterBase、SceneUIRegistrarを使う一方、`using HikanyanLibrary.UISystem;`がないため、たとえばnamespaceを`MyGame.UI`にすると型解決できません。

**改善:** パッケージ同梱テンプレートを既定で参照し、ユーザー編集時のみAssetsへ複製。生成コードのusingを明示する。namespaceの妥当性・残った置換トークン・出力先を生成前に検証する。

### R08 [P2] 上書き機能が衝突判定で先に拒否される

**根拠:** `UI/Editor/SceneUIMvpCreatorWindow.cs:406–421,472–490,627–656`。

既存の同じ出力ファイルを再生成しようとしても、プロジェクト全体の同名ファイル検出で止まり、Overwrite確認まで到達しません。namespaceが異なる同名クラスも区別していません。

**改善:** 正式な出力先自身は更新対象として扱い、別出力先の完全修飾型名の衝突と分離する。長期的には生成専用部分と使用者の実装を別ファイルにし、再生成で業務ロジックを上書きしない設計にする。

### R09 [P1] キー生成ツールが既存Addressablesの意味を変更する

**根拠:** `Keys/Editor/PrefabKeysGeneratorSettings.cs:15–16`、`PrefabBinderInitializer.cs:18–25`、`AddressableAssetsUtil.cs:59–70,162–166`。

- 既定の監視範囲がAssets全体で、対象Prefabのaddressをファイル名へ書き換えます。既存ゲームの階層付きaddressに依存するロードを壊し得ます。
- 別フォルダの同名Prefabも同じaddressになり、生成定数は辞書で1つにまとめられます。曖昧な参照をエラーにしません。
- AutoGenerateをOFFにしても、Addressablesの変更イベント経由では設定を確認せず生成します。
- 識別子検証はC#予約語を除外しません。`class.prefab`などから不正な定数宣言を生成できます。

**改善:** 既定は明示登録したUI範囲に限定。既存addressを保存し、GUID／AssetReferenceを正とする。衝突・不正名は処理前に報告し、全自動経路が同じ設定を尊重する。変更イベントから生成処理を呼び、生成処理もAddressablesを変更するため、再入防止・変更の集約・内容が同じ場合の書き込み省略も必要です。イベントの実発火回数はEditorで要確認です。

### R10 [P1・周辺部品] InfiniteScrollの再利用時にデータ更新が欠落

**根拠:** `Widgets/Scroll/InfiniteScroll.cs:123–161,195–204,222–231`。

初期生成時はIInfiniteScroll実装とUnityEventの両方へ通知しますが、再利用時はUnityEventしか呼びません。IInfiniteScrollだけを実装した使用者のセルは、スクロールで位置だけ変わり、表示データが更新されません。

**改善:** controller一覧を保持し、初期化・再利用・明示Refreshのすべてを共通Bind関数に通す。将来はデータ件数、キー、Bind／Unbindを持つ明示的なデータソースにする。非同期画像ロードにはセル再利用ごとのキャンセルと世代チェックを用意する。

### R11 [P2・周辺部品] ButtonCommonが標準Buttonと異なる入力判定

**根拠:** `Widgets/Button/ButtonCommon.cs:150–176`。

`_button.interactable`のみを見ているため、CanvasGroupによる非操作状態やButtonコンポーネント自体の無効化を十分に反映しません。左ボタンの検査もなく、右クリックでも独自イベント・音が動作し得ます。Submitによるゲームパッド／キーボード操作はPointerClick経由の共通処理に入りません。

**改善:** 有効状態と`Button.IsInteractable()`を使い、クリック確定の共通処理をButton.onClick等に集約する。Pointer固有の押下演出と確定操作を分離する。

### その他の改善項目

| 優先度 | 箇所 | 指摘・改善 |
|---|---|---|
| P2 | `UI/Samples~/Prefab/PopupMessageView.cs:12–18` | setterはTMPへ反映せず、Startだけで描画。Start後の再Bindに追従しない。表示メソッド／setterで即時反映する |
| P2 | `UI/Editor/SceneUIMvpCreatorWindow.cs:807–844` | 未完了生成をプロジェクト識別のないEditorPrefsの単一枠へ保存。同時作成で上書き、別プロジェクトで誤参照する条件がある。プロジェクト単位のキューと復旧導線を持つ |
| P2 | `Widgets/Button/UITheme.cs:22–31` | entries変更後も辞書キャッシュを無効化しない。InspectorやPreset変更を即時反映できる更新APIを用意する |
| P2 | `Widgets/Button/ThemeColorBinder.cs`、`UIThemeProvider.cs` | BinderがProviderより先にOnEnableすると購読されず、Provider生成後の再接続もない。初期化順と実際に購読した相手の解除を管理する |
| P2 | `Widgets/Text/TextScroller.cs` | tokenがDestroyにしか結び付かず、非表示でも非同期処理が継続し得る。再表示・テキスト変更・幅変更に応じた停止／再計算を用意する |
| P2 | `Widgets/Shader/UIHalftoneFadeController.cs:63–69,98–115` | ExecuteAlwaysで生成するMaterialを編集時に解放しない。Material差し替えでも旧所有物を解放しない。所有物のみを編集時も正しく破棄する |
| P2 | `Widgets/Scroll/DotIndicator.cs` のBuildDots | コンテナ内の全子を削除する。専用コンテナ必須の検証か、自身の生成物だけを対象にする |
| P3 | Runtimeコメント・未使用API | `:contentReference[...]`の残留、未使用_sortOrder、未使用InternalSetInitialState、コメントアウトされたDI実装を整理する |
| P3 | ローダー2実装 | UIとキー生成パッケージの責務を整理し、ロード処理を一本化する。キー生成のEditor機能にRuntimeローダーは必須ではない |

## 6. 設計上の評価と目指す責務分担

### UIManagerの責務を小さくする

現在はSingleton、ロード、生成、登録、型検索、寿命、タイムアウトを直接担当します。さらに履歴やモーダルを追加すると変更の影響範囲が広がります。最初から巨大なDI構成にする必要はありませんが、以下の境界は有効です。

| 要素 | 責務 | 持たせないもの |
|---|---|---|
| UIService | 使用者向けOpen／Close／結果待機 | Addressables固有のhandle操作 |
| UIRegistry／UICatalog | 画面ID、引数型、Prefab、レイヤー、再利用方針の定義 | ゲームのセーブデータ |
| IUIViewLoader | 生成・ロード・解放の所有権 | 画面履歴や業務ロジック |
| UIHandle | 一度開いた画面の識別、状態、Close、結果 | 保存用の永続ID |
| UINode | 状態遷移、操作直列化、寿命 | 個別画面のデータ取得 |
| Presenter | サービス呼び出し、表示状態の構築、購読管理 | セーブ形式やアセットロードの低レベル処理 |
| View | 描画とユーザー操作の通知 | グローバルManagerへの直接依存 |
| UIScope | Scene／プレイヤー／画面群の登録と終了処理 | 全ゲーム共通の無制限な参照保持 |

ロード実装を差し替え可能にすると、直接Prefab参照だけの小規模ゲームでも使えます。DIコンテナは任意のアダプターにし、手動構築でも成立させるのが導入しやすい形です。

`IUINode`がある一方、Managerは具体的なUINodeBaseへ依存しています。interfaceを増やすこと自体を目的にせず、ロードと状態遷移をテストする際に差し替えたい境界から抽象化してください。

### Singletonの実際の動作も考慮する

現在の依存Coreでは`Instance`を読むだけで、存在しないManagerを自動生成します。したがってRegistrarの「Instance != nullを待つ」は、設定済みManagerの準備完了を待つことを保証しません。自動生成Managerは_defaultRoot未設定で、Canvasも生成しません。

**推奨:** BootstrapでCanvas、EventSystem、Layer、Loaderを明示的に構成する。手軽さのためSingleton窓口を残す場合も、構築済みUIServiceを参照する窓口に限定し、準備不足を診断できるようにします。複数プレイヤーやAdditive Sceneにはスコープ指定を用意します。

## 7. データ管理: 「画面の引数」と「ゲームデータ」を分ける

| データ種別 | 例 | 推奨配置・寿命 |
|---|---|---|
| 画面定義 | ID、Prefab、レイヤー、遷移、再利用方針 | UICatalog／ScriptableObject。制作時データ |
| Open引数 | 対象アイテムID、初期タブ、確認文 | 型付きArgs。1回のOpenに対応 |
| 表示状態 | ロード中、一覧、選択行、エラー文 | Presenter管理のState。画面またはスコープ寿命 |
| ゲーム状態 | 所持金、インベントリ、進行度 | ゲーム側サービス／Repository。UIから独立 |
| 戻り値 | 確定／取消、選んだアイテム | 型付きResult。待機終了を必ず保証 |
| 永続設定 | 音量、言語、操作設定 | ゲーム側の保存サービス。UIは操作APIを呼ぶ |
| 表示スタイル | 色、余白、文字サイズ、演出時間 | Theme／Style定義。実行時状態との混在を避ける |

`Parameter`を必ず廃止する必要はありませんが、Argsへ意味を明確化するのがよいです。画面が開いている間のデータ変更は、もう一度Openする暗黙の挙動にせず、Refresh／State更新として定義してください。現在のOpenは既にIsOpen=trueなら引数変更を無視します。

ゲームのデータ保存機構そのものをUI基盤に含めると汎用性を下げます。必要なのは、UIから既存のゲームサービスへ接続する簡単な方法と、表示更新・購読解除の共通規約です。リアクティブライブラリを必須にせず、C#イベント等でも利用できる形を推奨します。

## 8. 使用者が直感的に量産するためのAPIと制作フロー

### 型・キー・引数の整合性を基盤側が保証する

現状の`OpenAsync<TPresenter,TParam>(string key, ...)`はTPresenterとTParamの対応を型制約で保証していません。文字列の参照先も別管理です。画面数が増えるほど、コピー時の取り違えが実行時エラーになります。

次は**未実装の目標API例**です。

```csharp
// GameUI.Inventoryが引数型と画面定義を保持する、生成済みの型付きキー
var handle = await ui.OpenAsync(
    GameUI.Inventory,
    new InventoryArgs(playerId),
    cancellationToken);

await handle.CloseAsync(cancellationToken);

// 確認ダイアログは画面内部のボタン操作を結果として返す
var result = await ui.ShowDialogAsync(
    GameUI.Confirm,
    new ConfirmArgs("購入しますか？"),
    cancellationToken);
```

同じ画面を重複表示するのか、既存画面を前面へ移すのか、データだけ更新するのかを画面定義で選びます。整数InstanceIDは内部識別に留め、使用者へは所有権・終了状態が分かるhandleを返します。保存やシーン間の復元にInstanceIDを使わないようにします。

### 標準の制作手順

1. Setup WizardでCanvas、EventSystem、UIService、レイヤー、保存先を作成する。
2. 「Create UI」でPage／Modal／HUD／ToastとScene／Prefabを選ぶ。
3. 名前とArgsを定義し、Presenter・View・画面定義を生成する。
4. Prefab Variantと標準部品を使い、Viewの参照をInspectorで設定する。
5. Validateで参照欠落、ID衝突、ロード設定、必須Canvas、namespaceを検査する。
6. 生成された型付きAPIから開く。サンプル画面で結果返却と戻る操作を確認する。

開発者が毎回設定する項目を減らすには、コード生成だけでなく、共通Prefab、Style、初期値、検証エラーの案内をセットで提供することが重要です。

### ゲーム用として優先したい機能

| 機能 | 推奨方針 |
|---|---|
| レイヤー | HUD／Page／Modal／Toast等を定義。Canvasの分割数は計測して決める |
| モーダル | 背面入力遮断とフォーカス復帰を開閉状態に連動 |
| 戻る操作 | 最前面の閉じられる画面を処理。閉じられない画面は明示指定 |
| ナビゲーション | Pageの履歴とModalの重なりを別管理 |
| 入力 | マウス／タッチ／ゲームパッドの確定経路を統一 |
| ポーズ中UI | 演出・タイムアウトのunscaled time方針を定義 |
| 解像度・言語 | Safe Area、CanvasScaler、長文／日本語／文字サイズ変更のサンプル |
| キャッシュ | Destroy／KeepAlive／Poolを明示指定。上限と解放タイミングを定義 |

汎用性のために初期版からすべてのUI技術へ対応する必要はありません。現状に合わせてuGUIを主対象とし、UI Toolkit対応はロード・View境界が安定してから判断する方が現実的です。

## 9. パフォーマンス評価

**現時点で「軽い／重い」を数値で断定できません。コード上の確実な無駄と、計測して判断すべき負荷を分けます。**

| 項目 | 静的評価 | 推奨対応 |
|---|---|---|
| ManagerのDictionary検索 | 通常は平均O(1)。毎フレームの全ノード走査はない | 現方式を維持してよい。古い登録の寿命だけ是正 |
| Addressables | R01の未解放参照は優先修正 | 所有権を明示し参照数を測る |
| 毎回Instantiate／Destroy | 開閉頻度とPrefabサイズに応じてスパイクの可能性 | 頻出UIのみ事前ロード／KeepAlive／Pool。全画面キャッシュは避ける |
| View探索 | Awake時のGetComponent探索は毎フレームではない | 高優先の最適化ではない。明示参照は設定検証にも役立つ |
| InfiniteScrollのリスト操作 | RemoveFirst後にAddLast(value)するためLinkedListNodeが再割り当てされる。逆方向も同様 | node自体を移動するかリングバッファ化。セル再利用だけではGCゼロにならない |
| InfiniteScrollの整列 | 後方向の再利用では後続セルの再整列が走る。通常の対象はプール数 | 可変高さと大ジャンプを計測。有限リストでは件数・境界も明示 |
| レイアウトの即時更新 | RequestRelayout(true)でCanvas全更新と即時Rebuildが前後にある | 同フレームの更新を集約し、必要な局所範囲に限定 |
| GradientImage | 中間キーごとの三角形分割と作業List生成がある | 動的変更頻度、頂点数、GCを計測。バッファ再利用と形状キャッシュを検討 |
| SimpleRoundedImage | 分割数が頂点数を増やす。作業Listの再利用は良い | 画面内の数と見た目から標準分割数を決める |
| Halftone Material | GraphicごとのMaterial複製でバッチ分断の可能性 | Frame Debuggerで確認。所有権修正後に共有可能範囲を判断 |
| ButtonCommon | 音が不要でも各ButtonにAudioSourceを追加し得る | 共通UI音サービスか、再生が必要なときだけ生成 |
| Editorキー生成 | 変更ごとに走査・書き込み・SaveAssets／Refresh | 変更集約、差分生成、再入防止、無変更時の早期終了 |

### 計測計画

対象端末、Unityバージョン、解像度、ビルド方式、画面の要素数を固定して記録します。EditorだけでなくDevelopment Playerでも測定します。

- HUDのみ、Modalを重ねた状態、大量一覧スクロール、ポーズ中、シーン往復を用意。
- Cold OpenとWarm Openを分け、Open完了時間、メインスレッド時間、GC Alloc、Canvas rebuild、頂点数、Draw Call、ロード参照数を確認。
- 開閉100回とシーン往復後のメモリを比較。キャッシュを残す仕様ならその上限内で安定するかを判定。
- 60fps対象の全フレーム予算16.67msから、ゲーム処理を考慮してUI予算を設定。これは測定結果ではありません。

## 10. 配布形態の評価

### 現在の導入案内には不整合がある

- READMEのmanifest例は`com.hikanyan.hikanyanlibrary.uisystem`ですが、実際のpackage名は`com.hikanyan.uisystem`です。
- ルートとサブディレクトリに同名package.jsonがあります。READMEはリポジトリルートを指定し、実装は`Packages/com.hikanyan.uisystem`内です。UPMの公開ルートを一本化すべきです。
- 現構成を維持するなら、導入先は `https://github.com/Hikanyan/UISystem.git?path=/Packages/com.hikanyan.uisystem` を基本にし、検証済みタグで固定する形が候補です。このURLでの新規導入確認は未実施です。
- Runtime asmdefはCoreとUniTaskを参照しますが、package.jsonのdependenciesにはAddressablesしかありません。READMEで先行導入を案内していても、初回セットアップとしての検出・復旧導線が不足します。Git依存はUPMで使える配布方式を確認し、利用者manifest／レジストリ導入の手順を正確に記載します。
- `Samples~`はあるもののpackage.jsonにsamples宣言がありません。スクリプトだけで、対応する動作Prefab／Sceneも同ディレクトリにはありません。Package Managerから導入し、そのまま再生できる形にします。
- SamplesはTMPやInput Systemにも依存します。サンプル単位で必要依存と有効化手順を明記します。
- MIT表記とルートLICENSEはありますが、サブフォルダを配布ルートにするならLICENSE、README、CHANGELOGも配布パッケージ内へ配置します。

### Unity対応バージョンを実態と一致させる

`unity: 2020.3`を宣言していますが、コードは`is not`やtarget-typed newを使用し、現在のCore依存は`FindFirstObjectByType`を使用しています。2020.3で現状のまま動く前提にはできません。Addressablesの要求版もUI側2.9.1とキー生成側1.19.19で異なります。異なる宣言だけで必ず競合するとは断定できませんが、実際に解決する版と互換性の検証が必要です。

まず現在のUnity 6環境を対応基準として検証し、必要が明確になった旧LTSのみ対応を追加することを推奨します。最低版・主要対応版・PlayerビルドをCIで検証し、その結果に合わせてpackage metadataを設定します。

### 推奨パッケージ構成

```text
com.hikanyan.uisystem/
  Runtime/             状態管理、型付きAPI、画面定義
  Editor/              作成・設定・検証ツールと既定テンプレート
  Tests/               EditMode／PlayMode
  Samples~/            Minimal、Dialog、Inventory等の動作サンプル
  Documentation~/      導入、データ連携、寿命、トラブル解決
  package.json / README.md / CHANGELOG.md / LICENSE

必要になった段階で分離:
  com.hikanyan.uisystem.addressables  ロード方式のアダプター
  com.hikanyan.uitools               テーマ・ボタン・描画等
```

初期段階でパッケージを細分化しすぎる必要はありません。先にasmdefと責務の境界を整え、依存を選択する需要が出た段階で物理分割します。

現行の.gitignoreはProjectSettingsとmanifest等を追跡対象外にしています。パッケージ専用リポジトリとしては選択肢ですが、再現可能な検証用Unityプロジェクトを別途用意する必要があります。UIToolsを製品に含めるなら、未追跡Assetsから正式なパッケージへ移す範囲も決めてください。

## 11. 段階的な改善計画

| 段階 | 実施内容 | 完了条件 |
|---|---|---|
| 1. 寿命を安定化 | R01〜R05、強制cleanup、ロード所有権、開閉状態機械 | 失敗／連打／破棄後にノード・購読・handleが残らない |
| 2. 初回導入を成立 | R07〜R09、配布ルート、依存、対応版、サンプル | 新規Unityプロジェクトで導入→生成→Open／Closeが手順通り完了 |
| 3. 量産APIを整備 | Catalog、型付きArgs／Result、Handle、Validate、生成コード分離 | 新しい画面を既存画面から安全に派生でき、キーと引数の不整合を検出 |
| 4. ゲーム共通機能 | Modal、戻る、フォーカス、Layer、Sceneスコープ | ゲームパッド・ポーズ・シーン遷移でも一貫した操作 |
| 5. 周辺部品・性能 | R10〜R11、テーマ、セルBind、計測と選択的再利用 | 代表画面で性能予算内、長時間運用でメモリが安定 |
| 6. 配布品質 | CI、CHANGELOG、移行ガイド、API安定化、リリースタグ | クリーン導入とPlayerビルドが再現でき、サンプルと説明が一致 |

現在のversion値は1.0.0ですが、APIや配布構成を大きく変える場合は既存利用者の有無を確認し、プレリリースとして育てるか、破壊的変更を明記した次のメジャー版へ進めるかを決めます。単に番号だけを下げない方が安全です。

## 12. 最低限追加したい検証

| 種別 | ケース | 期待する保証 |
|---|---|---|
| PlayMode | 通常Open→Close、100回反復 | 状態、登録、ロード参照、表示の整合 |
| PlayMode | Open中Close／Close中Open／連打 | 定義した順序で完了、Bind・Unbind・解放は過不足なし |
| PlayMode | 型不一致、Prefab Component欠落、ロード失敗 | 診断が具体的で、生成物とhandleが残らない |
| PlayMode | ロード／Bind／演出中キャンセル、外部Destroy | 終了種別が正しく伝わりcleanupが完了 |
| PlayMode | SceneUnload、Additive、同型重複 | スコープ終了後に古い登録なし、重複方針が一定 |
| PlayMode | TimeScale=0、非アクティブ配置 | タイムアウトと登録方針が説明通り |
| EditMode | 任意namespace、再生成、同名Prefab、予約語 | 正常なコードだけ生成、使用者コードと既存addressを保護 |
| PlayMode | IInfiniteScrollのみでセル実装 | 再利用時も正しいデータを描画 |
| PlayMode | CanvasGroup無効、右クリック、Submit | 入力・確定・効果音の意味が統一 |
| CI／導入 | クリーンな対応Unityでインストール、Sample Import、ビルド | 開発者固有のAssets／EditorPrefs／キャッシュなしで動作 |

状態機械とCatalogの検証はEditModeで高速に、Unityの有効化順・Destroy・Canvas・シーン寿命はPlayModeで検証する分担が適切です。

## 13. 推奨する最初の着手範囲

**最初の改善単位は「UIを開いて、閉じて、失敗しても確実に片付く」ことに絞るのが効果的です。** Addressables所有権、ノード状態機械、Bind解除、Scene登録寿命と対応するテストを一緒に整えます。

次に、新規プロジェクトで動く導入サンプルと生成ツールを完成させ、型付きの画面定義を導入します。この順序なら、現在のMVPの良さを残しながら、ゲームごとに再実装が必要な部分を減らせます。量産できる基盤の価値は、生成するコードの多さではなく、使用者が寿命・参照・入力・設定の失敗を毎回考えなくて済むことにあります。
