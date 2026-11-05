# Material Overridable VRM

Unity / URP向けに、VRM 0.x / 1.0のバイナリと差し替えMaterialを `.mouv` AssetBundleとしてexport / importするUPMパッケージです。

- Package ID: `com.baxter.material-override-vrm`
- Author: Baxter
- License: [zlib/png (Zlib)](LICENSE)
- 検証環境: Unity 2022.3.22f1 / URP 14.0.10、Unity 6000.3.13f1 / URP 17.3.0

## 導入・利用方法

導入・セットアップ・エクスポートの手順は[パッケージREADME](Packages/com.baxter.material-override-vrm/README.md)を参照してください。
`.mouv` を読み込むアプリを開発する方は、[Importガイド](Packages/com.baxter.material-override-vrm/README_import.md)を参照してください。

Git URLから導入する場合は、Package Managerの **Add package from git URL...** に以下の形式で指定します。

```text
https://github.com/malaybaku/MaterialOverrideVrm.git?path=/Packages/com.baxter.material-override-vrm#v0.3.2
```

パッケージ本体は `Packages/com.baxter.material-override-vrm/` にあります。

エクスポート用のテンプレートプロジェクトは [Examples](Examples/README.md) にあります。Unity 2022 / Unity 6それぞれの依存関係とURP設定、作業用シーンを用意しています。モデルは同梱していません。

## テンプレートプロジェクトのダウンロード

[v0.3.2リリース](https://github.com/malaybaku/MaterialOverrideVrm/releases/tag/v0.3.2)から、エクスポート用のテンプレートプロジェクトをzipでダウンロードできます。

- [Unity 2022.3.22f1用](https://github.com/malaybaku/MaterialOverrideVrm/releases/download/v0.3.2/MaterialOverrideVrm-Unity2022-v0.3.2.zip)
- [Unity 6000.3.13f1（Unity 6.3 LTS）用](https://github.com/malaybaku/MaterialOverrideVrm/releases/download/v0.3.2/MaterialOverrideVrm-Unity6-v0.3.2.zip)

## パッケージ単体ZIP

v0.3.1以降では、テンプレートプロジェクトとは別にパッケージ部分のzipも取得できます: [com.baxter.material-override-vrm-0.3.2.zip](https://github.com/malaybaku/MaterialOverrideVrm/releases/download/v0.3.2/com.baxter.material-override-vrm-0.3.2.zip)
UniVRM 0.131.4以降を導入済みのプロジェクトに対し、ZIPを展開後、UPMの **Add package from disk...** から展開先のpackage.jsonを指定できます。この方法でパッケージを導入する場合、ZIPの展開先は導入後も導入したプロジェクトから参照されます。フォルダパスが維持できる場所にパッケージを配置してください。
