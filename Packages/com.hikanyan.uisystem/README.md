# Hikanyan UI System 2.0 Preview

Unity 6000.3以降のuGUI向けUI基盤。まずUniTask 2.5.10をGit URLで導入してください。

`https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#a9e27c03d411d2fca01cc7410c24c97cd77cb539`

このパッケージは `https://github.com/Hikanyan/UISystem.git?path=/Packages/com.hikanyan.uisystem` から導入します。HikanyanLibrary-Coreは不要です。

Samplesの「Minimal Inventory and Dialog」をImportし、Input Systemを導入して`Minimal.unity`を再生してください。購入確認、所持金の更新、Cancel／戻る、フォーカス復帰を試せます。

制作時は`GameObject > Hikanyan > UI > Create UI Bootstrap`から開始します。

- `Documentation~/Guide.md`: 画面作成、データ連携、API契約
- `Documentation~/Migration-2.0.md`: 破壊的変更と移行
- `CHANGELOG.md`: 変更履歴
- `Tests/`: EditMode／PlayModeテスト。manifestのtestablesへ`com.hikanyan.uisystem`を追加

現在はpreviewです。Unity 6000.3.15f1で検証し、旧Unity／実機の描画性能は保証していません。
