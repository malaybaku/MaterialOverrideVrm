# Changelog

## 0.3.3

- UnityのToolsメニューをMaterial Override VRMへ統一しました。
- VRChat向けパッケージと同じメニューに表示し、Core側の項目は区切り線の下に配置するよう表示順を調整しました。
- 導入例とテンプレートプロジェクトの参照先をv0.3.3へ更新しました。

.mouvデータ形式、エクスポート・インポートの動作、依存パッケージの要件は変更していません。

## 0.3.2

- VPM用の配布URL・一覧URL・ドキュメントURLをマニフェストへ追加しました。IDは維持し、表示名をMaterial Overridable VRM (Core Library)へ変更して基盤パッケージであることを明示しました。
- UniVRMの自動的なUPM依存宣言を削除し、利用者がPackage Managerから手動で導入する方式へ変更しました。URPは通常のUPM依存宣言を維持します。
- UniGLTF / VRM-1.0 0.131.4以上が両方揃うまで、Runtime / Editorアセンブリをコンパイル対象から除外します。導入・更新APIを呼ぶコードは追加していません。
- UniVRMの対応判定に上限を設けません。ただし、将来版での動作を事前検証済みという意味ではありません。

.mouvデータ形式、エクスポート・インポートのC#実装は変更していません。

## 0.3.1

- package.jsonのversionを0.1.0から0.3.1へ修正し、リリース番号と一致させました。
- 導入例とテンプレートプロジェクトの参照先をv0.3.1へ更新しました。
- パッケージ単体ZIPとテンプレートプロジェクトZIPを生成・検査するスクリプトを追加しました。

## 0.3.0

VRM 0.x対応を含む既存リリース。過去の変更はGitHub Releasesを参照してください。
https://github.com/malaybaku/MaterialOverrideVrm/releases
