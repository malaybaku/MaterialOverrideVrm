# .mouvの読み込み（アプリ開発者向け）

この文書は、`.mouv` を読み込むUnityアプリを開発する方向けのAPIガイドです。
モデルをセットアップして `.mouv` を出力する手順は [README](README.md) を参照してください。

信頼できる作成元のファイルを使用してください。任意の入力Bundleの安全性は保証しません。

## 基本的な読み込み

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

v0.3.0の標準ImporterはVRM 0.x / 1.0の両方に対応します。VRM 0.xは元バイナリを保持したまま、UniVRMの移行機能で読み込みます。
`model.Instance` はどちらも `Vrm10Instance` です。移行時の再現範囲はUniVRMの移行機能に従います。

## 元Material / Textureの生成省略

このLoadAsyncは、未差し替えのmesh / slotが使う元Materialを解析し、完全に不要な元Materialの生成を省きます。
元Materialを一部のslotで使い続ける場合は保持し、必要な元TextureはUniVRMの生成キー単位で共有します。
元Materialが必要としないTextureは生成せず、VRMメタデータのサムネイルは保持します。
UniVRM内部のMaterial索引・Expression参照用に、元Materialを省略したモデルにはテクスチャなしの代替Materialを1個保持します。
元のVRMバイナリ、Bundle内のOverride Texture、使用中の元Textureはこの最適化の削減対象ではありません。

## 読み込み済みモデルへの適用

既存の `RuntimeGltfInstance` に対しては `MaterialOverrideApplicator.Apply(instance, data)` を使えます。
**instanceは必ずdata.VrmBytesと同じVRMから、構造を変更せずに読み込んだものを指定してください。**
VRM 0.xの場合は `Vrm10.LoadBytesAsync` の `canLoadVrm0X: true` で移行して読み込んだinstanceを使用してください。

**独自にUniVRMでロード・移行する場合は、必ず `data.VrmBytes` を複製して渡してください。**
UniVRMの座標変換は入力バッファを書き換えることがあります。Bundle内の配列を直接渡すと、後続の構造検証や同じBundleの再ロードに影響します。メタデータ取得用のロードや、Apply用データを準備する移行処理でも同様です。
標準の `MaterialOverrideImporter.LoadAsync` はこの複製を内部で行います。

```csharp
var instance = await Vrm10.LoadBytesAsync(
    (byte[])data.VrmBytes.Clone(), canLoadVrm0X: true, showMeshes: false);
var runtime = instance.GetComponent<RuntimeGltfInstance>();
var lease = MaterialOverrideApplicator.Apply(runtime, data);
runtime.ShowMeshes();
```

上記はv0.3.0のApplyを使う例です。差し替えが完了するまでモデルを非表示にしています。
索引やスロット数は検証しますが、別モデルとの同一性は自動判定しません。
返された `MaterialOverrideLease` をDisposeすると差し替えMaterialを解放し、そのMaterialを参照しているslotをNoneにします。元Materialは復元しません。
元の見た目のモデルが必要なら `data.VrmBytes` をUniVRMで新しくロードしてください。
Applyは既にロードされたモデルに適用するAPIなので、元Material / Textureの生成省略は行いません。
このAPIを直接使う場合、dataおよび元のBundleはleaseの寿命が終わるまで保持してください。
元モデルのリソースとGameObjectの破棄も呼び出し側で管理します。

## byte[]など独自の入力経路

LoadAsync(path)は標準の入力経路です。Bundleのbyte[]等を扱う場合は、LoadFromMemoryAsync、
UniVRMのロード、Applyを組み合わせた独自の所有ハンドルを利用者側で用意できます。
パスベースのBundle共有管理とMaterialOverrideModelの生成は内部実装であり、そのまま再利用する公開APIは提供していません。

## 既存アプリとの互換性

v0.3.0でも `.mouv` のデータ形式はv1のままです。VRMバイナリ・mesh / slotの対応表・差し替えマテリアルの格納形式は変更していません。
VRM 0.xの移行と移行後の構造に対するApplyを独自に実装済みのアプリは、この変更だけを理由に読み込み側パッケージを更新する必要はありません。
一方、v0.1.0 / v0.2.0の標準ImporterはVRM 0.xに対応していないため、標準Importerを使う場合はv0.3.0以降が必要です。

旧Exporterで出力したファイルには、内部Bundle名が固定のものがあります。これらは複数モデルの切り替えや同時保持で名前衝突する場合があるため、現行Exporterで再出力してください。
現行Exporterは内部Bundle名を `model_<GUID>.mouv` として出力ごとに一意にします。SOのアセット名や、利用者が指定する保存ファイル名とは別の名前です。
