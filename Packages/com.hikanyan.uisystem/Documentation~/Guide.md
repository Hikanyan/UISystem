# UISystem 利用ガイド

## 画面定義と型付きAPI

`UIDefinition`へID、Presenter付きPrefab、Layer、Reuse、Duplicateを設定し、`UICatalog.Screens`へ追加します。直接Prefab参照がある場合はそれを使用し、ない場合はAddressableKeyでロードします。

```csharp
public sealed class InventoryArgs : Parameter
{
    public string PlayerId { get; }
    public InventoryArgs(string playerId) => PlayerId = playerId;
}
// InventoryPresenter : PresenterBase<InventoryView, InventoryArgs>
var key = UIKey<InventoryArgs>.For<InventoryPresenter>("Inventory");
var handle = await ui.OpenAsync(key, new InventoryArgs("player-1"), token);
await handle.CloseAsync(token);
await handle.Closed;
```

キーの引数型とPresenterの引数型が異なるコードはコンパイルできません。CatalogのPrefab型はロード時にも検証します。Catalog Inspectorからキーを生成するときはPrefab参照が必要です。生成後にAddressableKey専用へ切り替えられます。生成された`.g.cs`と使用者のPresenter／Viewは分離されます。

## データの責務

Argsは1回の表示要求、Presenterは表示状態と購読、Viewは描画と操作通知を担当します。インベントリやセーブはゲーム側サービスへ置いてください。`UIStateStore<T>`は任意の表示状態コンテナです。

```csharp
protected override void OnBind()
{
    var state = Model.State;
    Render(state.Value);
    state.Changed += Render;
    Bindings.Add(() => state.Changed -= Render);
}
```

`Bindings`へ解除処理を登録すると、通常Close、Open失敗、演出キャンセル、破棄でも解除されます。非同期のデータ取得には`BindingToken`を渡すと表示終了時にキャンセルできます。ゲーム状態を保存する処理まで画面のtokenで無条件に打ち切るかどうかは、ゲームサービス側で決めます。

## 開閉とキャンセル

- APIはUnityのメインスレッドから呼びます。
- ノードはClosed → Opening → Open → Closing → Closedで遷移します。破棄後はDisposedです。
- 同一ノードの処理は直列化されます。Closeは進行中Openをキャンセルします。派生アニメーションは受け取ったtokenを尊重してください。
- 開閉フックから同じManagerのOpen／Close完了をawaitする再入呼び出しはしないでください。フックはその画面の初期化・演出に限定します。
- Close演出を開始した後の失敗・キャンセルでも最終的に閉じて解除します。呼び出し前にキャンセル済みなら状態を変更しません。
- 型付きOpenはManager単位でも直列化します。初版は正しい順序を優先するため、遅いロードが別のOpenを待たせます。複数プレイヤー等では別Managerを使用できます。
- `Duplicate=FocusExisting`では既存handleを返し、引数を更新しません。新しいデータはStateから更新するか、一度閉じて再Openしてください。
- 同じダイアログの結果待機を独立させたい場合は`Duplicate=Multiple`を使います。
- 外部コードからGameObjectを直接無効化せず、handleで閉じてください。外部Destroyはcleanupされます。
- `OnDestroy`を派生クラスでoverrideするときは必ずbaseを呼びます。

## 結果を返すダイアログ

`DialogPresenterBase<TView,TArgs,TResult>`を継承し、`OnDialogBind`で描画・購読を設定し、ボタンから`CompleteResult(value)`を呼びます。

```csharp
var key = UIDialogKey<ConfirmArgs, bool>.For<ConfirmPresenter>("Confirm");
bool accepted = await ui.ShowDialogAsync(key, new ConfirmArgs("購入しますか？"), token);
```

結果確定後はManagerがCloseします。結果未確定のまま戻る・破棄した場合はOperationCanceledExceptionです。「取消」を通常の結果として扱いたいボタンは`CompleteResult(false)`などを呼びます。

## 層・戻る・フォーカス

BootstrapはHUD／Page／Modal／Toastの順にレイヤーを作ります。最新Pageだけを描画・操作対象とし、前のPageは履歴として保持します。Backで最新Pageを閉じると前のPageを表示します。Modalは背面のRaycastとSelectable操作を遮断し、閉じると選択状態を復帰します。Toastは標準では入力を受けません。

`BackAsync`は最前面Modalを優先し、その次にPageを処理します。`CloseOnBack=false`の最前面画面がある場合は背面を閉じません。型付きOpen時にSelectableへCancel転送を設定します。独自入力や選択がない画面では、ゲームの入力処理から`BackAsync`を呼んでください。

履歴中のPageはCanvasGroupで隠しており、GameObjectのUpdateやゲームサービスの購読は停止しません。重いバックグラウンド更新はゲーム側で制御してください。

## 再利用とロード所有権

DestroyはClose後にロード参照ごと解放します。KeepAliveはClose時にUnbindし、非アクティブなノードをManagerに保存します。次回は再Bindし、新しいhandleを返します。古いhandleは新しい表示を閉じられません。

キャッシュ容量はManagerの`_cacheCapacity`（既定8画面）。満杯で別画面を追加するとキャッシュ全体を解放します。各IDにつき1個までです。シーンの予算に合わせて容量を設定し、不要になった時点で`ClearCache`を呼べます。汎用オブジェクトプールやLRUは未提供です。

`IUIViewLoader`を差し替える場合、LoadAsyncは所有する1インスタンスを返し、Releaseは未表示・部分初期化のノードにも対応してください。Managerは生成に用いたLoaderで解放します。組み込みAddressablesはLoadAssetの参照をインスタンスの寿命まで保持します。

## Scene UI

SceneUIRegistrarを使います。初期Closedが推奨です。非アクティブな子のRegistrarは、アクティブな親のUIScopeから登録できます。同一Managerでは同じPresenter型のScene UIを1個だけ登録でき、重複は登録前に例外になります。

`closeOnRegister=false`は既に構築済みの表示をOpenとして扱う互換モードで、ArgsのBindを自動実行しません。データを伴う初期表示には登録後の`OpenSceneAsync`を使ってください。登録解除はノードの寿命終了であり、再登録はできません。

複数Managerを置けますが、`Instance`は最初のManagerへの便宜的アクセスです。各UIScopeには明示的にManagerを設定してください。シーン寿命のManagerは`_persistent=false`にします。

## 見た目と制作

`UIFadeTransition`をPresenterと同じGameObjectへ追加すると、基底の演出フックでunscaled timeのフェードを利用できます。生成テンプレートの演出overrideを使う場合はbaseを呼ぶか独自演出を実装します。

UIBootstrapのSafeAreaはScreen Space Overlayを前提とします。World Spaceや分割画面では独自Rootを設定してください。角丸・グラデーション等は別パッケージUIToolsの任意機能です。

## 検証と制限

PlayModeでは連打、失敗、キャンセル、Scene登録、ポーズ中のタイムアウト、結果返却、再利用、Addressables参照解放を検証します。`WarmOpenCloseBenchmark`は`TestResults/UISystem-WarmOpenClose.txt`へ空Presenterの計測を出力します。実画面のFPSや実機性能を表す値ではありません。使用環境のGCカウンターが動作しない場合、割り当て量はunavailableと記録します。

旧Unity、UI Toolkit、WebGL／モバイル固有の検証、可変高さの有限仮想リスト、任意Prefabの自動View配線、一般的なPool、LRU、ローカライズサービスは今回の実装範囲に含みません。
