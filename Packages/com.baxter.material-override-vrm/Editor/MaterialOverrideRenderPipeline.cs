using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Baku.MaterialOverridableVrm.Editor
{
    /// <summary>URP preflight and explicit project configuration. No compile-time URP dependency.</summary>
    public static class MaterialOverrideRenderPipeline
    {
        public static RenderPipelineAsset ConfiguredPipeline => QualitySettings.renderPipeline
            ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;

        public static bool IsUniversal(RenderPipelineAsset asset)
        {
            for (var type = asset ? asset.GetType() : null; type != null; type = type.BaseType)
                if (type.FullName == "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset") return true;
            return false;
        }

        public static RenderPipelineAsset[] FindUrpAssets() => AssetDatabase.FindAssets("t:RenderPipelineAsset")
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal)
            .Select(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>).Where(IsUniversal).ToArray();

        /// <summary>Returns false after showing setup; the user exports again after changing pipelines.</summary>
        internal static bool CheckForExportWithDialog()
        {
            if (IsUniversal(ConfiguredPipeline)) return true;
            MaterialOverrideUrpSetupWindow.Open();
            return false;
        }

        /// <summary>Assign Graphics default and clear the active Quality override. Other Quality overrides are preserved.</summary>
        public static void ApplyUrp(RenderPipelineAsset asset)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Change the render pipeline in Edit Mode after compilation and import finish.");
            if (!IsUniversal(asset) || !AssetDatabase.Contains(asset))
                throw new ArgumentException("Select a saved Universal Render Pipeline asset.", nameof(asset));
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")
                .Concat(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")).ToArray();
            Undo.RecordObjects(settings, "Use URP for Material Override Export");
            GraphicsSettings.defaultRenderPipeline = asset;
            QualitySettings.renderPipeline = null;
            foreach (var setting in settings) EditorUtility.SetDirty(setting);
            AssetDatabase.SaveAssets();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
    }
}
