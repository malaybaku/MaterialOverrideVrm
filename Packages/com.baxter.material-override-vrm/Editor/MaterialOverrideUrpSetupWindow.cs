using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Baku.MaterialOverridableVrm.Editor
{
    public static class MaterialOverrideUrpSetupWindow
    {
        public const string Folder = "Assets/MaterialOverrideVrmGenerated";
        public const string PipelinePath = Folder + "/Pipeline.asset";
        public const string RendererPath = Folder + "/Renderer.asset";
        internal static Type UrpType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("UnityEngine.Rendering.Universal." + name)).FirstOrDefault(t => t != null);

        [MenuItem("Tools/Material Overridable VRM/URP Export Setup")]
        public static void Open()
        {
            var japanese = MaterialOverrideEditorLanguage.IsJapaneseEditor();
            var isUrp = MaterialOverrideRenderPipeline.IsUniversal(MaterialOverrideRenderPipeline.ConfiguredPipeline);
            var reason = japanese
                ? (isUrp ? "URP設定を作成・適用します。" : "現在のRender PipelineがURPではないため、URP設定を作成・適用します。")
                : (isUrp ? "Create and apply URP settings." : "The current Render Pipeline is not URP. Create and apply URP settings.");
            var details = japanese
                ? "\n\nAssets/MaterialOverrideVrmGenerated/ に設定アセットを作成します。\nOKを選択するとGraphicsの既定値を変更し、現在のQualityのRender Pipeline上書きを解除します。"
                : "\n\nSettings assets will be created in Assets/MaterialOverrideVrmGenerated/.\nSelecting OK changes the Graphics default and clears the current Quality's Render Pipeline override.";
            if (EditorUtility.DisplayDialog(japanese ? "URP設定" : "URP Setup", reason + details,
                "OK", japanese ? "キャンセル" : "Cancel"))
            {
                try { CreateAndApply(); }
                catch (Exception e)
                {
                    var error = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
                    EditorUtility.DisplayDialog(japanese ? "URP設定の適用失敗" : "URP Setup Failed", error.Message, "OK");
                    Debug.LogException(e);
                }
            }
        }

        public static RenderPipelineAsset CreateAndApply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Wait for Edit Mode, compilation and import to finish.");
            var pipelineType = UrpType("UniversalRenderPipelineAsset");
            var rendererType = UrpType("UniversalRendererData");
            if (pipelineType == null || rendererType == null)
                throw new InvalidOperationException("Install the Universal RP package compatible with this Unity Editor, then retry.");
            var existing = AssetDatabase.LoadMainAssetAtPath(PipelinePath);
            var rendererExisting = AssetDatabase.LoadMainAssetAtPath(RendererPath);
            if ((existing && !pipelineType.IsInstanceOfType(existing)) || (rendererExisting && !rendererType.IsInstanceOfType(rendererExisting)))
                throw new InvalidOperationException("The generated URP paths contain unrelated assets. Move them manually before retrying: " + Folder);
            if (existing && !rendererExisting)
                throw new InvalidOperationException("Generated URP Renderer is missing. Restore it before retrying: " + RendererPath);
            if (!existing)
            {
                var parent = "Assets";
                foreach (var part in Folder.Substring(7).Split('/'))
                {
                    if (!AssetDatabase.IsValidFolder(parent + "/" + part)) AssetDatabase.CreateFolder(parent, part);
                    parent += "/" + part;
                }
                var renderer = rendererExisting;
                if (!renderer)
                    renderer = (UnityEngine.Object)pipelineType.GetMethod("CreateRendererAsset", BindingFlags.Static | BindingFlags.NonPublic)
                        .Invoke(null, new object[] { RendererPath, Enum.Parse(pipelineType.Assembly.GetType("UnityEngine.Rendering.Universal.RendererType"), "UniversalRenderer"), false, "Renderer" });
                var rendererSettings = new SerializedObject(renderer);
                rendererSettings.FindProperty("m_RenderingMode").intValue = 2; // Forward+ in URP 14 and 17.
                rendererSettings.ApplyModifiedPropertiesWithoutUndo();
                var pipeline = (RenderPipelineAsset)pipelineType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new object[] { renderer });
                var settings = new SerializedObject(pipeline);
                settings.FindProperty("m_RequireDepthTexture").boolValue = true;
                settings.FindProperty("m_SupportsHDR").boolValue = true;
                settings.FindProperty("m_MSAA").intValue = 1;
                settings.FindProperty("m_ShadowDistance").floatValue = 10;
                settings.FindProperty("m_MainLightShadowsSupported").boolValue = true;
                settings.FindProperty("m_AdditionalLightShadowsSupported").boolValue = true;
                settings.FindProperty("m_SoftShadowsSupported").boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                existing = pipeline;
            }
            MaterialOverrideRenderPipeline.ApplyUrp((RenderPipelineAsset)existing);
            return (RenderPipelineAsset)existing;
        }
    }
}
