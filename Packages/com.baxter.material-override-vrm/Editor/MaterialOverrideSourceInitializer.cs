using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Baku.MaterialOverridableVrm.Editor
{
    [InitializeOnLoad]
    public static class MaterialOverrideSourceInitializer
    {
        static MaterialOverrideSourceInitializer() => MaterialOverrideSource.EditorReset += OnReset;

        private static void OnReset(MaterialOverrideSource source) => TryAssignSourceVrm(source);

        /// <summary>Assign only when a single original VRM can be identified. Never overwrite a setting.</summary>
        public static bool TryAssignSourceVrm(MaterialOverrideSource source)
        {
            if (!source || source.SourceVrm) return false;
            var asset = FindOriginalVrm(source.gameObject);
            if (!asset)
            {
                foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
                {
                    var candidate = FindOriginalVrm(renderer.gameObject);
                    if (!candidate) continue;
                    if (asset && candidate != asset) return false;
                    asset = candidate;
                }
            }
            if (!asset) return false;
            Undo.RecordObject(source, "Assign source VRM");
            source.SourceVrm = asset;
            if (PrefabUtility.IsPartOfPrefabInstance(source)) PrefabUtility.RecordPrefabInstancePropertyModifications(source);
            EditorUtility.SetDirty(source);
            return true;
        }

        private static GameObject FindOriginalVrm(GameObject instance)
        {
            var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance);
            var path = AssetDatabase.GetAssetPath(original ? original : instance);
            return string.Equals(Path.GetExtension(path), ".vrm", StringComparison.OrdinalIgnoreCase)
                ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
        }
    }
}
