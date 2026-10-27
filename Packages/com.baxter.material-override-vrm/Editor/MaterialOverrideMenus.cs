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
                if (!MaterialOverrideRenderPipeline.CheckForExportWithDialog()) return;
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
