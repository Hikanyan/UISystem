# Minimal Inventory and Dialog

1. Unity 6000.3のPackage ManagerでInput Systemを導入してください。
2. Active Input HandlingをInput System PackageまたはBothにし、必要ならEditorを再起動してください。
3. このサンプルをImportし、`Minimal.unity`を再生します。

Inventoryで購入ボタンを押すとModalが開き、Confirmで所持金が10減ります。Cancel／Escape（UI Cancel）では購入せず、前のボタンへフォーカスが戻ります。Add 10 coinsは画面外のState更新をViewへ反映する例です。

Canvas、Catalog、prototype、EventSystemは実行時に作成します。Addressables設定、TMPのフォント追加、HikanyanLibrary-Coreは不要です。製品ではprototypeをPrefab化し、Catalogから型付きキーを生成してください。

Testsには購入／キャンセル／フォーカス復帰のPlayModeテストが含まれます。
