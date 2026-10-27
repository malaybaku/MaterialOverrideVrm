using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Baku.MaterialOverridableVrm.Editor
{
    public static class MaterialOverrideMenus
    {
        public static bool IsExporting { get; private set; }

        public static async Task ExportWithDialogAsync(MaterialOverrideSource source)
        {
            if (!source || IsExporting) return;
            IsExporting = true;
            try
            {
                if (!CheckExportPrerequisites(EditorUserBuildSettings.activeBuildTarget)) return;
                var path = EditorUtility.SaveFilePanel("Export Material Overridable VRM", "", source.name, "mouv");
                if (string.IsNullOrEmpty(path)) return;
                await MaterialOverrideExporter.ExportAsync(source, path);
                Debug.Log($"Exported: {path}", source);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Material Override Export Failed", e.Message, "OK");
                Debug.LogException(e, source);
            }
            finally { IsExporting = false; }
        }

        internal static bool IsWindowsTarget(BuildTarget target) =>
            target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64;

        internal static bool CheckExportPrerequisites(BuildTarget target)
        {
            if (!IsWindowsTarget(target))
            {
                var japanese = MaterialOverrideEditorLanguage.IsJapaneseEditor();
                var message = japanese
                    ? $"現在のビルドターゲットは {target} です。\n\nこのまま出力したBundleは、Windowsアプリで読み込めません。続行しますか？"
                    : $"The current build target is {target}.\n\nThe exported bundle cannot be loaded by Windows applications. Continue?";
                if (!EditorUtility.DisplayDialog(japanese ? "Windows以外へのエクスポート" : "Non-Windows Export",
                    message, "OK", japanese ? "キャンセル" : "Cancel")) return false;
            }
            return MaterialOverrideRenderPipeline.CheckForExportWithDialog();
        }

        [MenuItem("Tools/Material Overridable VRM/Import Bundle (Play Mode)")]
        private static async void ImportBundle()
        {
            if (!Application.isPlaying) { EditorUtility.DisplayDialog("Material Overridable VRM", "Enter Play Mode before importing.", "OK"); return; }
            var path = EditorUtility.OpenFilePanel("Import Material Overridable VRM", "", "mouv");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                var result = await MaterialOverrideImporter.LoadAsync(path);
                Selection.activeGameObject = result.Root;
            }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
