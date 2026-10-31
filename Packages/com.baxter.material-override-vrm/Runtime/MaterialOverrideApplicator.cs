using System;
using System.Collections.Generic;
using UniGLTF;
using UnityEngine;

namespace Baku.MaterialOverridableVrm
{
    /// <summary>
    /// Applies to an unmodified RuntimeGltfInstance loaded from data.VrmBytes.
    /// MaterialBinding expressions are intentionally unsupported.
    /// </summary>
    public static class MaterialOverrideApplicator
    {
        public static void Validate(MaterialOverrideData data)
        {
            if (!data) throw new ArgumentNullException(nameof(data));
            if (data.FormatVersion != MaterialOverrideData.CurrentVersion)
                throw new NotSupportedException($"Unsupported format version: {data.FormatVersion}");
            if (data.VrmBytes == null || data.VrmBytes.Length == 0)
                throw new ArgumentException("VRM bytes are missing.");
            if (data.Bindings == null) throw new ArgumentException("Bindings are missing.");
            using var parsed = new GlbLowLevelParser("", data.VrmBytes).Parse();
            var used = new HashSet<(int, int)>();
            foreach (var binding in data.Bindings)
            {
                if (binding == null || !binding.Material)
                    throw new ArgumentException("An override material is missing.");
                if (binding.MeshIndex < 0 || binding.MeshIndex >= parsed.GLTF.meshes.Count)
                    throw new ArgumentException($"Invalid mesh index: {binding.MeshIndex}");
                var mesh = parsed.GLTF.meshes[binding.MeshIndex];
                if (binding.MaterialSlot < 0 || binding.MaterialSlot >= mesh.primitives.Count)
                    throw new ArgumentException($"Invalid material slot: {binding.MeshIndex}/{binding.MaterialSlot}");
                if (!used.Add((binding.MeshIndex, binding.MaterialSlot)))
                    throw new ArgumentException($"Duplicate target: {binding.MeshIndex}/{binding.MaterialSlot}");
                if (!parsed.GLTF.nodes.Exists(n => n.mesh == binding.MeshIndex))
                    throw new ArgumentException($"Mesh {binding.MeshIndex} has no node.");
            }
        }

        /// <summary>Validate every target before changing any renderer. Dispose releases overrides, without restoring originals.</summary>
        public static MaterialOverrideLease Apply(RuntimeGltfInstance instance, MaterialOverrideData data)
        {
            if (!instance) throw new ArgumentNullException(nameof(instance));
            if (instance.GetComponent<MaterialOverrideLease>())
                throw new InvalidOperationException("Overrides are already applied to this instance.");
            Validate(data);
            using var input = new OverrideVrmData(data.VrmBytes);
            var parsed = input.Data;
            if (instance.Nodes.Count != parsed.GLTF.nodes.Count)
                throw new ArgumentException("The loaded instance does not match the source VRM node count.");

            var replacements = new Dictionary<Renderer, Material[]>();
            var assignedSlots = new Dictionary<Renderer, HashSet<int>>();
            foreach (var binding in data.Bindings)
            {
                for (var i = 0; i < parsed.GLTF.nodes.Count; i++)
                {
                    if (parsed.GLTF.nodes[i].mesh != binding.MeshIndex) continue;
                    var node = instance.Nodes[i];
                    var renderer = node ? node.GetComponent<Renderer>() : null;
                    if (!renderer || renderer.sharedMaterials.Length != parsed.GLTF.meshes[binding.MeshIndex].primitives.Count)
                        throw new ArgumentException($"Renderer layout mismatch at node {i}.");
                    if (!replacements.ContainsKey(renderer))
                    {
                        replacements.Add(renderer, renderer.sharedMaterials);
                        assignedSlots.Add(renderer, new HashSet<int>());
                    }
                    replacements[renderer][binding.MaterialSlot] = binding.Material;
                    assignedSlots[renderer].Add(binding.MaterialSlot);
                }
            }

            var clones = new Dictionary<Material, Material>();
            MaterialOverrideLease lease = null;
            try
            {
                foreach (var binding in data.Bindings)
                    if (!clones.ContainsKey(binding.Material))
                        clones.Add(binding.Material, new Material(binding.Material));
                foreach (var pair in replacements)
                    foreach (var slot in assignedSlots[pair.Key]) pair.Value[slot] = clones[pair.Value[slot]];
                lease = instance.gameObject.AddComponent<MaterialOverrideLease>();
                lease.Initialize(new List<Renderer>(replacements.Keys), new List<Material>(clones.Values));
                foreach (var pair in replacements) pair.Key.sharedMaterials = pair.Value;
                return lease;
            }
            catch
            {
                if (lease) lease.Dispose();
                else foreach (var clone in clones.Values) MaterialOverrideLease.DestroyObject(clone);
                throw;
            }
        }
    }
}
