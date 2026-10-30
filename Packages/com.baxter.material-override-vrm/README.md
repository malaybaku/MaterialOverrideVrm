# Material Overridable VRM

UPM ID: `com.baxter.material-override-vrm` / Author: Baxter / License: [zlib/png (Zlib)](LICENSE)

VRM 0.x / 1.0の元バイナリとUnityの差し替えマテリアルを、1つのAssetBundleに格納します。
出力拡張子は `.mouv`（Material Override Unity VRM）です。Unity向けの形式であり、通常のVRMファイルとして扱うことはできません。
Unity 2022.3.22f1 / URP 14.0.10とUnity 6000.3.13f1 / URP 17.3.0で検証しています（Windows Editor）。
モデル、検証用Shader、サンプルプロジェクトはこのパッケージには含みません。

## 導入

必要な環境はURPとUniVRM 0.131.4（`com.vrmc.gltf` / `com.vrmc.vrm`）です。
依存宣言はUnity 2022対応の14.0.10です。Unity 6ではプロジェクトのmanifestでURP 17.3.0を直接指定してください。
導入だけではRender Pipelineの設定は切り替わりません。未適用の場合は後述のセットアップ補助を使用します。

まずプロジェクトの `Packages/manifest.json` の `dependencies` に以下を追加してください。

```json
"com.vrmc.gltf": "https://github.com/vrm-c/UniVRM.git?path=/Packages/UniGLTF#v0.131.4",
"com.vrmc.vrm": "https://github.com/vrm-c/UniVRM.git?path=/Packages/VRM10#v0.131.4"
```

次にPackage Managerの **Add package from git URL...** で下記を指定して導入します。

```text
https://github.com/malaybaku/MaterialOverrideVrm.git?path=/Packages/com.baxter.material-override-vrm#v0.2.0
```

URPアセットをGraphics / Quality設定へ割り当てたプロジェクトを使用してください。
非URP状態では保存ダイアログより前にexportを停止し、URPが必要な旨をダイアログで表示します。
Forward+、Depth Texture有効、HDR有効、MSAA無効、影の距離10を設定し、以後は同じアセットを再利用します。
Graphicsの既定値を変更し、現在のQualityのRender Pipeline上書きを解除します。他のQuality設定は変更しません。
補助機能は既存Materialの変換を行いません。URPパッケージはUPM依存として取得されます。適用後にもう一度Exportしてください。
同じダイアログは `Tools > Material Overridable VRM > URP Export Setup` から開けます。

Bundleビルド中だけURPのUnused Variants / Unused Post Processing Variants / Screen Coord Override Variantsの削減を無効化し、終了時に元へ戻します。

## Export

1. 元のVRM 0.x / 1.0ファイルをAssetsへ配置し、UniVRMで読み込みます。
2. シーン上のモデルのrootに `MaterialOverrideSource` を追加します。
3. アタッチ時に子のMeshRenderer / SkinnedMeshRendererの全slot（非アクティブな子も含む）がBindingsへ自動登録されます。
   `Source Vrm` もPrefabの由来から自動設定されます（Variant対応）。特定できない場合は手動指定し、差し替えたい行だけ設定します。
   Bindingsを展開するとRenderer名（Face / Body等）のブロックが表示され、その中に `Material 0` 等の番号順で以下のペアが並びます。
   - `Current`: 現在のMaterial（読み取り専用）。
   - `Override`: ユーザーが編集する差し替え先。初期値Noneの行は差し替えず、exportの対応表にも入りません。
4. Inspectorの `Export...` で保存先を選び、`.mouv` を出力します。

同じMaterialは複数のBindingに指定できます。
一覧の手動追加・削除・並べ替えはできません。階層やMaterial構成を変えたら `Reload Bindings` で不足分の追加と不要分の削除を一括実行します。
BundleのBuildTargetは現在のEditorの選択を使用します。

### Editorでのプレビュー

シーン上のモデルを選択してPlay Modeに入り、Inspectorの `Preview Material Override (Play Mode)` を押すと、Bindingsのセットアップに基づいてマテリアルを適用します。
マテリアル適用後は、 `Stop Material Override Preview` を行うか、Play Modeを終了することで元のマテリアル表示に戻ります。
プレビュー中はBindingsの設定は変更できなくなります。

## .mouvを読み込むアプリを開発する方へ

読み込みAPIとリソース管理については [Importガイド（アプリ開発者向け）](README_import.md) を参照してください。
このガイドは `.mouv` を読み込むアプリを作る場合にのみ必要です。モデルのセットアップ・エクスポートには、ここまでの手順をご利用ください。

## 保証しない動作

- VRMのMaterialBindingを使うExpression（色・UV等）。動かない／意図と異なる結果になっても補正しません。
- 異なるBuildTarget、Unity世代、URP世代、Graphics API間でのシェーダー互換性（個別検証が必要）。
- 任意の入力Bundleの安全性。信頼できる作成元のファイルを使用してください。

検証範囲はWindows Editor / StandaloneWindows64向けBundleです。Playerビルドは未検証です。
lilToon 2.4.1も、Opaque / Cutout、albedo・色・Shadow・Rim・Emissionを設定した部分差し替えについて、
Unity 2022 / Unity 6のexportとUnity 6でのimport・描画・Play Modeを確認しています。
他のlilToonバージョン、Transparent / Outline / Fur等の未検証の構成まで保証するものではありません。
任意のShaderを参照できますが、そのShaderの対応URP・Graphics APIとShader Variantが出力先で利用できる必要があります。

BundleにはScriptableObject、VRMバイナリ、差し替えMaterialとそのShader・Texture等の依存アセットが入ります。
読み込み済みのVRM GameObject、Mesh、UniVRMコンポーネントは含めません。
差し替えMaterialが必要とするTextureはBundleに含みます。
元VRMのimporterサブアセットを直接参照すると、VRMのGameObjectまで依存するためexportで拒否される場合があります。
その場合は必要なTextureを独立したPNG等のアセットとして保存し、差し替えMaterialから参照してください。

## ライセンス

Copyright (c) 2026 Baxter。zlib/pngライセンスで配布します。
UniVRM、Unity、利用するShader・モデル等には、それぞれのライセンスと利用条件が適用されます。
