using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Baku.MaterialOverridableVrm.Editor
{
    // Only URP's feature-dependent stripping is disabled, only during this bundle build.
    // Unity platform stripping and third-party shader preprocessors remain in effect.
    internal sealed class MaterialOverrideShaderVariants : IDisposable
    {
        private readonly SerializedObject _settings;
        private readonly Dictionary<string, bool> _previous = new Dictionary<string, bool>();
        public MaterialOverrideShaderVariants()
        {
            var type = MaterialOverrideUrpSetupWindow.UrpType("UniversalRenderPipelineGlobalSettings");
            var ensure = type?.GetMethod("Ensure", BindingFlags.Static | BindingFlags.NonPublic);
            var args = ensure?.GetParameters();
            var values = args == null ? null : Array.ConvertAll(args, p => p.DefaultValue);
            var global = ensure?.Invoke(null, values) as UnityEngine.Object;
            if (!global) throw new InvalidOperationException("URP Global Settings are unavailable; cannot protect export shader variants.");
            _settings = new SerializedObject(global);
            var property = _settings.GetIterator();
            while (property.Next(true))
                if (property.propertyType == SerializedPropertyType.Boolean &&
                    (property.name == "m_StripUnusedVariants" || property.name == "m_StripUnusedPostProcessingVariants" || property.name == "m_StripScreenCoordOverrideVariants"))
                {
                    _previous.Add(property.propertyPath, property.boolValue);
                    property.boolValue = false;
                }
            if (_previous.Count == 0) throw new InvalidOperationException("Unsupported URP shader stripping settings layout.");
            MaterialOverrideVariantFilter.Begin();
            try
            {
                _settings.ApplyModifiedPropertiesWithoutUndo();
                RefreshBuildSettings();
            }
            catch { Dispose(); throw; }
        }
        // URP caches feature lists and writes derived prefilter data to pipeline assets.
        // Refresh both on entry and exit so later builds cannot inherit export settings.
        internal static void RefreshBuildSettings()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEditor.Rendering.Universal.ShaderBuildPreprocessor")).FirstOrDefault(t => t != null);
            var gather = type?.GetMethod("GatherShaderFeatures", BindingFlags.Static | BindingFlags.NonPublic);
            if (gather == null) throw new InvalidOperationException("Unsupported URP shader build settings API.");
            gather.Invoke(null, new object[] { Debug.isDebugBuild });
        }
        public void Dispose()
        {
            MaterialOverrideVariantFilter.End();
            try
            {
                _settings.Update();
                foreach (var value in _previous) _settings.FindProperty(value.Key).boolValue = value.Value;
                _settings.ApplyModifiedPropertiesWithoutUndo();
                RefreshBuildSettings();
            }
            finally
            {
                AssetDatabase.SaveAssetIfDirty(_settings.targetObject);
                foreach (var asset in MaterialOverrideRenderPipeline.FindUrpAssets()) AssetDatabase.SaveAssetIfDirty(asset);
                _settings.Dispose();
            }
        }
    }
}
