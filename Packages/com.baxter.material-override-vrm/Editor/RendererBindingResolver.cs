using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UniGLTF;
using UniVRM10;
using UnityEditor;
using UnityEngine;

namespace Baku.MaterialOverridableVrm.Editor
{
    public static class RendererBindingResolver
    {
        public static MaterialOverrideBinding[] Resolve(MaterialOverrideSource source, byte[] bytes, RuntimeGltfInstance reference)
        {
            if (source.RendererBindings == null) throw new ArgumentException("Bindings are missing.");
            if (source.RendererBindings.Length == 0)
                return source.Bindings?.Where(b => b == null || b.Material).Select(b => b == null ? null : new MaterialOverrideBinding
                { MeshIndex = b.MeshIndex, MaterialSlot = b.MaterialSlot, Material = b.Material }).ToArray();
            using var input = new OverrideVrmData(bytes);
            var parsed = input.Data;
            var result = new List<MaterialOverrideBinding>();
            foreach (var binding in source.RendererBindings)
            {
                if (binding == null) throw new ArgumentException("A binding is missing.");
                if (!binding.ReplacementMaterial) continue;
                if (!binding.Renderer)
                    throw new ArgumentException("Select a renderer for each material replacement.");
                var renderer = binding.Renderer;
                if (!renderer.transform.IsChildOf(source.transform))
                    throw new ArgumentException($"Renderer '{renderer.name}' is outside the export root.");
                var materials = renderer.sharedMaterials;
                if (binding.MaterialSlot < 0 || binding.MaterialSlot >= materials.Length || materials[binding.MaterialSlot] != binding.TargetMaterial)
                    throw new ArgumentException($"The selected slot on '{renderer.name}' no longer contains the target material. Select the slot again.");
                var node = ResolveNode(source, renderer, reference);
                var mesh = parsed.GLTF.nodes[node].mesh;
                if (mesh < 0) throw new ArgumentException($"Renderer '{renderer.name}' has no source glTF mesh.");
                result.Add(new MaterialOverrideBinding { MeshIndex = mesh, MaterialSlot = binding.MaterialSlot, Material = binding.ReplacementMaterial });
            }
            return result.ToArray();
        }

        private static int ResolveNode(MaterialOverrideSource source, Renderer renderer, RuntimeGltfInstance reference)
        {
            var runtime = source.GetComponent<RuntimeGltfInstance>();
            if (runtime)
            {
                for (var i = 0; i < runtime.Nodes.Count; i++)
                    if (runtime.Nodes[i] == renderer.transform) return CheckLayout(renderer, reference, i);
                throw new ArgumentException("Renderer was not loaded from the source VRM.");
            }
            var path = AssetDatabase.GetAssetPath(source.SourceVrm);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var original = EditorUtility.IsPersistent(renderer) ? renderer : PrefabUtility.GetCorrespondingObjectFromOriginalSource(renderer);
            if (!original || AssetDatabase.GetAssetPath(original) != path || !prefab)
                throw new ArgumentException($"Renderer '{renderer.name}' must originate from Source VRM. Keep its prefab connection.");
            var sourcePath = SiblingPath(original.transform, prefab.transform);
            for (var i = 0; i < reference.Nodes.Count; i++)
                if (SiblingPath(reference.Nodes[i], reference.transform) == sourcePath)
                    return CheckLayout(original, reference, i);
            throw new ArgumentException($"Cannot map renderer '{renderer.name}' to a source VRM node.");
        }

        private static int CheckLayout(Renderer renderer, RuntimeGltfInstance reference, int node)
        {
            if (node >= reference.Nodes.Count) throw new ArgumentException("Source VRM node layout changed.");
            var expected = reference.Nodes[node].GetComponent<Renderer>();
            var mesh = GetMesh(renderer);
            var expectedMesh = expected ? GetMesh(expected) : null;
            if (!expected || renderer.GetType() != expected.GetType() || !mesh || !expectedMesh ||
                mesh.vertexCount != expectedMesh.vertexCount || mesh.subMeshCount != expectedMesh.subMeshCount ||
                renderer.sharedMaterials.Length != expected.sharedMaterials.Length)
                throw new ArgumentException($"Renderer '{renderer.name}' does not match the original VRM layout.");
            return node;
        }

        private static Mesh GetMesh(Renderer renderer) => renderer is SkinnedMeshRenderer skin ? skin.sharedMesh
            : renderer.GetComponent<MeshFilter>() ? renderer.GetComponent<MeshFilter>().sharedMesh : null;

        private static string SiblingPath(Transform transform, Transform root)
        {
            var indices = new List<int>();
            while (transform && transform != root) { indices.Add(transform.GetSiblingIndex()); transform = transform.parent; }
            if (!transform) throw new ArgumentException("Renderer is outside the original VRM hierarchy.");
            indices.Reverse();
            return string.Join("/", indices);
        }

        public static async Task ConvertLegacyBindingsAsync(MaterialOverrideSource source)
        {
            if (source.RendererBindings.Any(b => b != null && b.ReplacementMaterial))
                throw new InvalidOperationException("Configured renderer bindings already exist.");
            var bytes = File.ReadAllBytes(AssetDatabase.GetAssetPath(source.SourceVrm));
            var model = await Vrm10.LoadBytesAsync((byte[])bytes.Clone(), canLoadVrm0X: true, showMeshes: false, awaitCaller: new ImmediateCaller());
            try
            {
                using var input = new OverrideVrmData(bytes);
                var parsed = input.Data;
                var reference = model.GetComponent<RuntimeGltfInstance>();
                var converted = new List<RendererMaterialOverrideBinding>();
                foreach (var old in source.Bindings)
                {
                    Renderer found = null;
                    foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
                    {
                        var node = ResolveNode(source, renderer, reference);
                        if (parsed.GLTF.nodes[node].mesh == old.MeshIndex) { found = renderer; break; }
                    }
                    if (!found || old.MaterialSlot < 0 || old.MaterialSlot >= found.sharedMaterials.Length)
                        throw new ArgumentException("A legacy binding cannot be mapped to a renderer.");
                    converted.Add(new RendererMaterialOverrideBinding { Renderer = found, TargetMaterial = found.sharedMaterials[old.MaterialSlot],
                        MaterialSlot = old.MaterialSlot, ReplacementMaterial = old.Material });
                }
                Undo.RecordObject(source, "Convert material override bindings");
                source.RendererBindings = converted.ToArray();
                source.RefreshRendererBindings();
                source.Bindings = Array.Empty<MaterialOverrideBinding>();
                if (PrefabUtility.IsPartOfPrefabInstance(source)) PrefabUtility.RecordPrefabInstancePropertyModifications(source);
                EditorUtility.SetDirty(source);
            }
            finally
            {
                if (model)
                {
                    model.GetComponent<RuntimeGltfInstance>()
                        .TransferOwnership((key, value) => UnityEngine.Object.DestroyImmediate(value));
                    UnityEngine.Object.DestroyImmediate(model.gameObject);
                }
            }
        }
    }
}
