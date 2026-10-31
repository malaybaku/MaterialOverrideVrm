# エクスポート用テンプレートプロジェクト

VRMを持ち込んでマテリアルを設定・プレビュー・エクスポートするための、URPが適用済みのほぼ空のプロジェクトです。モデルや差し替えマテリアルは含みません。

| プロジェクト | Unity | URP | UniVRM |
| --- | --- | --- | --- |
| [Unity2022](Unity2022/) | 2022.3.22f1 | 14.0.10 | 0.131.4 |
| [Unity6](Unity6/) | 6000.3.13f1（Unity 6.3 LTS） | 17.3.0 | 0.131.4 |

1. 使用するプロジェクトフォルダをUnity Hubへ追加し、対応するバージョンで開きます。初回のパッケージ取得にはインターネット接続とGitが必要です。
2. `Assets/Scenes/Export.unity` を開きます。
3. VRM 0.x / 1.0ファイルをAssetsへ追加し、シーンの原点へ配置します。
4. モデルのrootへ `MaterialOverrideSource` を追加し、BindingsのOverrideを設定します。
5. Play Modeでプレビューし、Edit Modeへ戻って `Export...` から `.mouv` を出力します。

詳しいセットアップ・制約は[パッケージREADME](../Packages/com.baxter.material-override-vrm/README.md)を参照してください。
