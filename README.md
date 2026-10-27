# Material Overridable VRM

Unity / URP向けに、VRM 1.0のバイナリと差し替えMaterialを `.mouv` AssetBundleとしてexport / importするUPMパッケージです。

- Package ID: `com.baxter.material-override-vrm`
- Author: Baxter
- License: [zlib/png (Zlib)](LICENSE)
- 検証環境: Unity 2022.3.22f1 / URP 14.0.10、Unity 6000.3.13f1 / URP 17.3.0

## 導入・利用方法

依存関係、Inspectorの使い方、import API、検証範囲は[パッケージREADME](Packages/com.baxter.material-override-vrm/README.md)を参照してください。

Git URLから導入する場合は、Package Managerの **Add package from git URL...** に以下の形式で指定します。

```text
https://github.com/OWNER/REPOSITORY.git?path=/Packages/com.baxter.material-override-vrm#TAG
```

`OWNER` / `REPOSITORY` / `TAG` は公開先と実在するタグに置き換えてください。

パッケージ本体は `Packages/com.baxter.material-override-vrm/` にあります。サンプルプロジェクトやモデルは同梱していません。
