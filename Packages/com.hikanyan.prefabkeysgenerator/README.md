# Prefab Keys Generator 2.0 Preview

UISystem 2.0.0-preview.1とUniTaskを先に導入してください。Unity 6000.3が対象です。

`HikanyanLaboratory > Prefab Keys Generator`でUI専用のAssetsサブフォルダを指定し、実行します。Assets全体の自動登録はできません。自動生成は既定OFFです。設定は`ProjectSettings/HikanyanPrefabKeys.asset`へ保存されます。

既存アドレスを保持し、新規登録にはGUIDを使用します。同名Prefab、アドレス重複、不正識別子は書き込み前に検出します。予約語は`@`でエスケープされます。生成結果が同一ならファイルを書き換えません。

旧Runtimeローダーは非推奨の互換窓口です。`HikanyanLibrary.UISystem.AddressablePrefabLoader`を使用してください。
