using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Baku.MaterialOverridableVrm.Editor
{
    // Preserve the union of source-project features and the VMagicMirror target profile.
    // VMagicMirror uses Forward+, SSAO, lights/shadows/cookies, but not APV, decals,
    // light layers or reflection-probe blending/box projection/atlas.
    public sealed class MaterialOverrideVariantFilter : IPreprocessShaders
    {
        internal static ShaderKeyword[] Excluded;
        public static long VariantsRemoved { get; private set; }
        public static long VariantsSeen { get; private set; }
        public int callbackOrder => int.MinValue;

        internal static void Begin()
        {
            if (Excluded != null) throw new InvalidOperationException("A Material Override bundle build is already active.");
            var active = MaterialOverrideRenderPipeline.ConfiguredPipeline;
            if (!MaterialOverrideRenderPipeline.IsUniversal(active)) throw new InvalidOperationException("An active URP asset is required.");
            var assets = new[] { active };
            bool Enabled(string name) => assets.Any(a => {
                using (var s = new SerializedObject(a)) {
                    var p = s.FindProperty(name);
                    return p == null || (p.propertyType == SerializedPropertyType.Boolean ? p.boolValue : p.intValue != 0);
                }
            });
            bool HasApv() => assets.Any(a => {
                using (var s = new SerializedObject(a)) {
                    var p = s.FindProperty("m_LightProbeSystem");
                    // URP 14 has no APV pipeline setting; unknown later layouts are kept.
                    return p == null ? !Application.unityVersion.StartsWith("2022.") : p.intValue != 0;
                }
            });
            var decals = assets.Any(a => {
                using (var s = new SerializedObject(a)) {
                    var list = s.FindProperty("m_RendererDataList");
                    if (list == null) return true;
                    for (int i=0; i<list.arraySize; i++) {
                        var renderer = list.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (!renderer) continue;
                        using (var r = new SerializedObject(renderer)) {
                            var features = r.FindProperty("m_RendererFeatures");
                            if (features == null) return true;
                            for (int j=0; j<features.arraySize; j++) {
                                var feature = features.GetArrayElementAtIndex(j).objectReferenceValue;
                                if (feature && feature.GetType().Name != "ScreenSpaceAmbientOcclusion") return true;
                            }
                        }
                    }
                    return false;
                }
            });
            var names = new List<string>();
            if (!HasApv()) names.AddRange(new[] { "PROBE_VOLUMES_L1", "PROBE_VOLUMES_L2" });
            if (!decals) names.AddRange(new[] { "_DBUFFER_MRT1", "_DBUFFER_MRT2", "_DBUFFER_MRT3" });
            if (!Enabled("m_SupportsLightLayers")) names.Add("_LIGHT_LAYERS");
            if (!Enabled("m_ReflectionProbeBlending")) names.Add("_REFLECTION_PROBE_BLENDING");
            if (!Enabled("m_ReflectionProbeBoxProjection")) names.Add("_REFLECTION_PROBE_BOX_PROJECTION");
            if (!Enabled("m_ReflectionProbeAtlas")) names.Add("_REFLECTION_PROBE_ATLAS");
            Excluded = names.Select(n => new ShaderKeyword(n)).ToArray();
            VariantsRemoved = VariantsSeen = 0;
        }
        internal static void End() { Excluded = null; }
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (Excluded == null) return;
            VariantsSeen += data.Count;
            for (int i = data.Count-1; i >= 0; i--)
                foreach (var keyword in Excluded)
                    if (data[i].shaderKeywordSet.IsEnabled(keyword)) { data.RemoveAt(i); VariantsRemoved++; break; }
        }
    }
}
