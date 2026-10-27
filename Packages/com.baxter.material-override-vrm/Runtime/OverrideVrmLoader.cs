using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniGLTF;
using UniVRM10;
using UnityEngine;

namespace Baku.MaterialOverridableVrm
{
    /// <summary>Skips originals that no non-overridden primitive uses. Does not modify the embedded VRM.</summary>
    internal static class OverrideVrmLoader
    {
        internal static async Task<Vrm10Instance> LoadAsync(MaterialOverrideData data, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            using var parsed = new GlbLowLevelParser("", data.VrmBytes).Parse();
            var vrm = Vrm10Data.Parse(parsed);
            if (vrm == null) throw new InvalidDataException("The embedded file is not a VRM 1.0 model.");
            var targets = new HashSet<(int, int)>(data.Bindings.Select(b => (b.MeshIndex, b.MaterialSlot)));
            var needed = new HashSet<int>();
            foreach (var node in parsed.GLTF.nodes)
            {
                if (node.mesh < 0) continue;
                var primitives = parsed.GLTF.meshes[node.mesh].primitives;
                for (var slot = 0; slot < primitives.Count; slot++)
                    if (!targets.Contains((node.mesh, slot)) && primitives[slot].material >= 0)
                        needed.Add(primitives[slot].material.Value);
            }

            var generator = Vrm10MaterialDescriptorGeneratorUtility.GetValidVrm10MaterialDescriptorGenerator();
            var external = new Dictionary<SubAssetKey, UnityEngine.Object>();
            Material placeholder = null;
            PrunedImporter importer = null;
            RuntimeGltfInstance runtime = null;
            var transferred = false;
            try
            {
                for (int i = 0; i < parsed.GLTF.materials.Count; i++)
                {
                    if (needed.Contains(i)) continue;
                    // Preserve material-index lookup and Expression references without constructing originals.
                    if (!placeholder)
                    {
                        var shader = data.Bindings.Length > 0 ? data.Bindings[0].Material.shader : generator.GetGltfDefault().Shader;
                        placeholder = new Material(shader) { name = "MaterialOverride unused-original placeholder" };
                    }
                    external.Add(generator.Get(parsed, i).SubAssetKey, placeholder);
                }
                importer = new PrunedImporter(vrm, external, new NeededTextures(vrm, needed));
                IAwaitCaller caller = Application.isPlaying ? (IAwaitCaller)new RuntimeOnlyAwaitCaller() : new ImmediateCaller();
                // UniVRM cannot cancel during LoadAsync. Check after load and clean up the hidden model.
                runtime = await importer.LoadAsync(caller);
                if (!runtime) throw new InvalidDataException("VRM import failed.");
                if (placeholder) runtime.AddResource(placeholder);
                transferred = true;
                ct.ThrowIfCancellationRequested();
                var instance = runtime.GetComponent<Vrm10Instance>();
                if (!instance) throw new InvalidDataException("VRM instance is missing.");
                return instance;
            }
            catch
            {
                // LoadAsync may fail after creating a hierarchy or transferring resource ownership.
                var root = runtime ? runtime.gameObject : importer?.PartialRoot;
                if (root) MaterialOverrideLease.DestroyModelRoot(root);
                throw;
            }
            finally
            {
                importer?.Dispose();
                if (!transferred && placeholder) MaterialOverrideLease.DestroyObject(placeholder);
            }
        }

        private sealed class PrunedImporter : Vrm10Importer
        {
            internal GameObject PartialRoot => Root;
            internal PrunedImporter(Vrm10Data vrm, IReadOnlyDictionary<SubAssetKey, UnityEngine.Object> external,
                ITextureDescriptorGenerator textures) : base(vrm, externalObjectMap: external, useControlRig: true)
            {
                TextureDescriptorGenerator = textures;
            }
        }

        private sealed class NeededTextures : ITextureDescriptorGenerator
        {
            private readonly TextureDescriptorSet _textures = new TextureDescriptorSet();
            internal NeededTextures(Vrm10Data vrm, HashSet<int> needed)
            {
                var data = vrm.Data;
                foreach (var index in needed)
                {
                    var material = data.GLTF.materials[index];
                    if (UniGLTF.Extensions.VRMC_materials_mtoon.GltfDeserializer.TryGet(material.extensions, out var mtoon))
                    {
                        foreach (var entry in Vrm10MToonTextureImporter.EnumerateAllTextures(data, material, mtoon))
                            _textures.Add(entry.Item2.Item2);
                    }
                    else
                        foreach (var entry in GltfPbrTextureImporter.EnumerateAllTextures(data, index))
                            _textures.Add(entry.Item2);
                }
                if (Vrm10TextureDescriptorGenerator.TryGetMetaThumbnailTextureImportParam(data, vrm.VrmExtension, out var thumbnail))
                    _textures.Add(thumbnail.Item2);
            }
            public TextureDescriptorSet Get() => _textures;
        }
    }
}
