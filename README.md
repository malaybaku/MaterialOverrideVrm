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
https://github.com/malaybaku/MaterialOverrideVrm.git?path=/Packages/com.baxter.material-override-vrm#v0.2.0
```

パッケージ本体は `Packages/com.baxter.material-override-vrm/` にあります。

エクスポート用のテンプレートプロジェクトは [Examples](Examples/README.md) にあります。Unity 2022 / Unity 6それぞれの依存関係とURP設定、作業用シーンを用意しています。モデルは同梱していません。

## テンプレートプロジェクトのダウンロード

[v0.2.0リリース](https://github.com/malaybaku/MaterialOverrideVrm/releases/tag/v0.2.0)から、エクスポート用のテンプレートプロジェクトをzipでダウンロードできます。

- [Unity 2022.3.22f1用](https://github.com/malaybaku/MaterialOverrideVrm/releases/download/v0.2.0/MaterialOverrideVrm-Unity2022-v0.2.0.zip)
- [Unity 6000.3.13f1（Unity 6.3 LTS）用](https://github.com/malaybaku/MaterialOverrideVrm/releases/download/v0.2.0/MaterialOverrideVrm-Unity6-v0.2.0.zip)
