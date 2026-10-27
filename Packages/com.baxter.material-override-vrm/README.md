# Material Overridable VRM

UPM ID: `com.baxter.material-override-vrm` / Author: Baxter / License: [zlib/png (Zlib)](LICENSE)

VRM 1.0の元バイナリとUnityの差し替えマテリアルを、1つのAssetBundleに格納します。
出力拡張子は `.mouv`（Material Override Unity VRM）です。Unity向けの形式であり、通常のVRMファイルとして扱うことはできません。
Unity 2022.3.22f1 / URP 14.0.10とUnity 6000.3.13f1 / URP 17.3.0で検証しています（Windows Editor）。
2022製BundleのUnity 6での読み込み・描画も、非公開の3モデルと検証用Shaderで確認しています。
モデル、検証用Shader、サンプルプロジェクトはこのパッケージには含みません。

## 導入

必要な環境はURPとUniVRM 0.131.4（`com.vrmc.gltf` / `com.vrmc.vrm`）です。
本パッケージはVMagicMirror向けアバターの作成を目的とし、URPを必須依存として宣言しています。
依存宣言はUnity 2022対応の14.0.10です。Unity 6ではプロジェクトのmanifestでURP 17.3.0を直接指定してください。
導入だけではRender Pipelineの設定は切り替わりません。未適用の場合は後述のセットアップ補助を使用します。

まずプロジェクトの `Packages/manifest.json` の `dependencies` に以下を追加してください。

```json
"com.vrmc.gltf": "https://github.com/vrm-c/UniVRM.git?path=/Packages/UniGLTF#v0.131.4",
"com.vrmc.vrm": "https://github.com/vrm-c/UniVRM.git?path=/Packages/VRM10#v0.131.4"
```

次にPackage Managerの **Add package from disk...** で、本パッケージの `package.json` を指定します。
GitHub公開後は **Add package from git URL...** も使用できます。
パッケージを公開リポジトリの `Packages/com.baxter.material-override-vrm/` に置く構成で、URLは次の形式です。

```text
https://github.com/malaybaku/MaterialOverrideVrm.git?path=/Packages/com.baxter.material-override-vrm#v0.1.0
```

このURLは `v0.1.0` タグを指定しています。タグ公開後に導入できます。
Git依存の取得にはGitが必要です。Unity CLI LoopやlilToonはパッケージの依存ではありません。

URPアセットをGraphics / Quality設定へ割り当てたプロジェクトを使用してください。
非URP状態では保存ダイアログより前にexportを停止し、URPが必要な旨をダイアログで表示します。
「OK」または「キャンセル」の2択を表示します。
このダイアログはEditorの表示言語が日本語なら日本語、それ以外（言語を取得できない場合を含む）は英語で表示します。Inspectorの文言は英語です。
公式URPの生成機能で `Assets/MaterialOverrideVrmGenerated/` にPipelineとRendererを作成します。
Forward+、Depth Texture有効、HDR有効、MSAA無効、影の距離10を設定し、以後は同じアセットを再利用します。
Graphicsの既定値を変更し、現在のQualityのRender Pipeline上書きを解除します。他のQuality設定は変更しません。
補助機能は既存Materialの変換を行いません。URPパッケージはUPM依存として取得されます。適用後にもう一度Exportしてください。
同じダイアログは `Tools > Material Overridable VRM > URP Export Setup` から開けます。
VMagicMirror独自のRenderer Featureは含まず、表示の完全一致を保証するものではありません。

Bundleビルド中だけURPのUnused Variants / Unused Post Processing Variants / Screen Coord Override Variantsの削減を無効化し、終了時（失敗時も）に元へ戻します。
同時に、VMagicMirrorと出力元プロジェクトの双方で使用しないAPV・Decal・Light Layers・Reflection Probeの拡張機能の組み合わせを削減し、過剰なコンパイルを避けます。出力時に有効なURPアセットで使用する機能は保持します。
Unityのプラットフォーム別削減やShader独自の削減は引き続き有効です。任意のShaderの全バリアントを保持する保証はありません。
この対策は既存URP設定でのExportにも適用します。非公開モデルのlilToon 2.4.1 Opaque/Cutout検証では、通常のURP削減と比べたBundleサイズ増加はUnity 2022で約2.56%、Unity 6で約15.23%でした。
Shader次第でビルド時間・Bundleサイズが増えるため、実際に使うShader構成で確認してください。

## Export

1. 元のVRM 1.0ファイルをAssetsへ配置し、UniVRMで読み込みます。
2. シーン上のモデルのrootに `MaterialOverrideSource` を追加します。
3. アタッチ時に子のMeshRenderer / SkinnedMeshRendererの全slot（非アクティブな子も含む）がBindingsへ自動登録されます。
   `Source Vrm` もPrefabの由来から自動設定されます（Variant対応）。特定できない場合は手動指定し、差し替えたい行だけ設定します。
   Bindingsを展開するとRenderer名（Face / Body等）のブロックが表示され、その中に `Material 0` 等の番号順で以下のペアが並びます。
   - `Current`: 現在のMaterial（読み取り専用）。
   - `Override`: ユーザーが編集する差し替え先。初期値Noneの行は差し替えず、exportの対応表にも入りません。
4. Inspectorの `Export...` で保存先を選び、`.mouv` を出力します。

すべてNoneなら差し替えなしでexportできます。同じMaterialを複数のBindingに指定できます。
一覧の手動追加・削除・並べ替えはできません。階層やMaterial構成を変えたら `Reload Bindings` で不足分の追加と不要分の削除を一括実行します。
Renderer・対象Material・slotが一致する既存行の差し替え設定は保持します。slotの対象Materialが変わった場合、新しい行の差し替え先はNoneです。
Rendererは元VRMからのPrefab接続を保持してください（Prefab Variant・シーンインスタンスにも対応）。
UniVRMのruntime importで生成したrootは、RuntimeGltfInstanceのnode情報から解決します。
export時に元VRMのmesh番号へ変換するため、シーン上の名前変更・兄弟順変更には依存しません。
root外のRendererや、設定後に対象Materialが変わったslotはexport前にエラーになります。
既存の数値設定は非表示で保持され、Inspectorの `Convert Legacy Bindings` で新方式へ変換できます。
新しいRenderer設定がある場合はそちらを優先します。変換完了時には旧設定を消去します。
Bundleのデータ形式は従来どおり `MeshIndex` / `MaterialSlot` / 差し替えMaterial参照で、Renderer参照は含みません。
シーン上で手動変更したMeshやTransformは出力されません。出力対象の構造は常に元VRMのバイナリが決定します。
URPが有効なEdit Modeでexportしてください。BundleのBuildTargetは現在のEditorの選択を使用します。

## Import

Unityのメインスレッドから呼び出してください。

```csharp
using Baku.MaterialOverridableVrm;

var model = await MaterialOverrideImporter.LoadAsync(path, cancellationToken);
// model.Root / model.Instance を利用します。
model.Dispose(); // モデルと所有するマテリアル・Bundle参照を解放
```

Play Modeでは `Tools > Material Overridable VRM > Import Bundle (Play Mode)` でも確認できます。
GameObjectを直接Destroyした場合も、所有コンポーネントがリソースを解放します。
同一パスのBundleを複数回読む場合は参照カウントで共有し、各モデルの差し替えマテリアルは複製します。

このLoadAsyncは、未差し替えのmesh / slotが使う元Materialを解析し、完全に不要な元Materialの生成を省きます。
元Materialを一部のslotで使い続ける場合は保持し、必要な元TextureはUniVRMの生成キー単位で共有します。
元Materialが必要としないTextureは生成せず、VRMメタデータのサムネイルは保持します。
UniVRM内部のMaterial索引・Expression参照用に、元Materialを省略したモデルにはテクスチャなしの代替Materialを1個保持します。
元のVRMバイナリ、Bundle内のOverride Texture、使用中の元Textureはこの最適化の削減対象ではありません。

既存の `RuntimeGltfInstance` に対しては `MaterialOverrideApplicator.Apply(instance, data)` を使えます。
**instanceは必ずdata.VrmBytesと同じVRMから、構造を変更せずに読み込んだものを指定してください。**
索引やスロット数は検証しますが、別モデルとの同一性は自動判定しません。
返された `MaterialOverrideLease` をDisposeすると差し替えMaterialを解放し、そのMaterialを参照しているslotをNoneにします。元Materialは復元しません。
元の見た目のモデルが必要なら `data.VrmBytes` をUniVRMで新しくロードしてください。
Applyは既にロードされたモデルに適用するAPIなので、元Material / Textureの生成省略は行いません。
このAPIを直接使う場合、dataおよび元のBundleはleaseの寿命が終わるまで保持してください。
元モデルのリソースとGameObjectの破棄も呼び出し側で管理します。

LoadAsync(path)は標準の入力経路です。Bundleのbyte[]等を扱う場合は、LoadFromMemoryAsync、
UniVRMのロード、Applyを組み合わせた独自の所有ハンドルを利用者側で用意できます。
パスベースのBundle共有管理とMaterialOverrideModelの生成は内部実装であり、そのまま再利用する公開APIは提供していません。

## 保証しない動作

- VRMのMaterialBindingを使うExpression（色・UV等）。動かない／意図と異なる結果になっても補正しません。
- VRM 0.xの読み込み・移行。
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

Copyright (c) 2026 Baxter。zlib/pngライセンス（SPDX: `Zlib`）で配布します。
全文は [LICENSE](LICENSE)、ライセンス原文の参照先は [zlib License](https://www.zlib.net/zlib_license.html) です。
製品ドキュメントでの謝辞は任意です。ソース配布等の条件はLICENSEを参照してください。
UniVRM、Unity、利用するShader・モデル等には、それぞれのライセンスと利用条件が適用されます。
