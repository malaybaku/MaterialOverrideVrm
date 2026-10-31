# Material Overridable VRM

UPM ID: `com.baxter.material-override-vrm` / Author: Baxter / License: [zlib/png (Zlib)](LICENSE)

VRM 0.x / 1.0のマテリアルをUnityで差し替え、対応アプリで読み込める `.mouv` ファイルとして出力するパッケージです。
`.mouv` はUnity向けのAssetBundle形式で、通常のVRMファイルとしては扱えません。
モデルやサンプルプロジェクトは付属しません。

## 導入

検証済みの環境は以下のとおりです。

- Unity 2022.3.22f1 / URP 14.0.10
- Unity 6000.3.13f1 / URP 17.3.0
- UniVRM 0.131.4

まず `Packages/manifest.json` の `dependencies` に以下を追加してください。

```json
"com.vrmc.gltf": "https://github.com/vrm-c/UniVRM.git?path=/Packages/UniGLTF#v0.131.4",
"com.vrmc.vrm": "https://github.com/vrm-c/UniVRM.git?path=/Packages/VRM10#v0.131.4"
```

次にPackage Managerの **Add package from git URL...** で下記を指定します。

```text
https://github.com/malaybaku/MaterialOverrideVrm.git?path=/Packages/com.baxter.material-override-vrm#v0.3.0
```

URPも自動で導入されます。Unity 6ではプロジェクトのmanifestでURP 17.3.0を指定してください。

### URPの設定

URPを有効にしたプロジェクトで使用してください。未設定の場合は、エクスポート時に表示されるダイアログから設定できます。
`Tools > Material Overridable VRM > URP Export Setup` からも同じ設定を開けます。

この操作はGraphicsと現在のQualityのRender Pipeline設定を変更します。既存マテリアルの変換は行いません。
設定後、もう一度エクスポートしてください。

## セットアップとエクスポート

1. 元のVRM 0.x / 1.0ファイルをAssetsへ配置し、読み込まれたモデルをシーンに置きます。
2. モデルのルートに `MaterialOverrideSource` を追加します。`Source Vrm` とマテリアルの一覧（Bindings）が自動設定されます。`Source Vrm` が空の場合は元のVRMファイルを指定してください。
3. Bindingsを展開し、差し替えたい箇所の `Override` にマテリアルを指定します。Face / Body等のRendererごとに並び、`Current` で現在のマテリアルを確認できます。`Override` がNoneの箇所は元のままです。同じマテリアルを複数箇所に指定できます。
4. Edit ModeでInspectorの `Export...` を押し、保存先を選んで `.mouv` を出力します。

モデルの階層やマテリアル構成を変えた場合は、`Reload Bindings` で一覧を更新してください。
出力先のプラットフォームは、Unity Editorで選択中のBuildTargetを使用します。読み込み先のアプリに合わせて設定してください。

### プレビュー

モデルを選択してPlay Modeに入り、Inspectorの `Preview Material Override (Play Mode)` を押すと差し替え後の見た目を確認できます。
`Stop Material Override Preview` を押すか、Play Modeを終了すると元に戻ります。プレビュー中はBindingsを編集できません。

## 利用上の注意

- 差し替えマテリアルには、読み込み先のURP・Graphics APIに対応したShaderを使用してください。UnityやURPのバージョン、プラットフォームが異なる場合の互換性は個別に確認が必要です。
- lilToon 2.4.1はOpaque / Cutoutでの部分差し替えを確認しています。Transparent / Outline / Fur等や、他バージョンは未検証です。
- 表情によるマテリアルの色・UV等の変更（MaterialBindingを使うExpression）は動作を保証しません。
- 元VRMに含まれるTextureを差し替えマテリアルから直接参照すると、エクスポートできない場合があります。その場合はTextureを独立したPNG等として保存して参照してください。

検証範囲はWindows Editor / StandaloneWindows64向けの出力です。Unity 2022で出力したファイルのUnity 6での読み込み・描画・Play Modeも確認しています。Playerビルドは未検証です。

## .mouvを読み込むアプリを開発する方へ

読み込みAPIは [Importガイド（アプリ開発者向け）](README_import.md) を参照してください。モデルのセットアップ・エクスポートだけを行う場合、このガイドを読む必要はありません。

## ライセンス

Copyright (c) 2026 Baxter。zlib/pngライセンスで配布します。
UniVRM、Unity、利用するShader・モデル等には、それぞれのライセンスと利用条件が適用されます。
