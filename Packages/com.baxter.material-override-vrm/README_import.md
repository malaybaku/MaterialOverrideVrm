# .mouvの読み込み（アプリ開発者向け）

この文書は、`.mouv` を読み込むUnityアプリを開発する方向けのAPIガイドです。
モデルをセットアップして `.mouv` を出力する手順は [README](README.md) を参照してください。

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
