using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UniGLTF;
using UniVRM10;
using UnityEngine;

namespace Baku.MaterialOverridableVrm
{
    public sealed class MaterialOverrideModel : IDisposable
    {
        public Vrm10Instance Instance { get; }
        public GameObject Root => Instance ? Instance.gameObject : null;
        internal MaterialOverrideModel(Vrm10Instance instance) => Instance = instance;
        public void Dispose()
        {
            if (!Root) return;
            var lease = Root.GetComponent<MaterialOverrideLease>();
            if (lease) lease.Dispose();
            MaterialOverrideLease.DestroyModelRoot(Root);
        }
    }

    /// <summary>Main-thread API. VRM 1.0 and migrated VRM 0.x. Bundle target must match the running platform.</summary>
    public static class MaterialOverrideImporter
    {
        private sealed class Entry { public AssetBundle Bundle; public int Users; }
        private static readonly Dictionary<string, Entry> Bundles = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        public static async Task<MaterialOverrideModel> LoadAsync(string path, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            path = Path.GetFullPath(path);
            if (!Bundles.TryGetValue(path, out var entry))
            {
                var bundle = AssetBundle.LoadFromFile(path);
                if (!bundle) throw new IOException($"Could not load AssetBundle: {path}");
                entry = new Entry { Bundle = bundle };
                Bundles.Add(path, entry);
            }
            entry.Users++;
            Vrm10Instance instance = null;
            var transferred = false;
            try
            {
                var roots = entry.Bundle.LoadAllAssets<MaterialOverrideData>();
                if (roots.Length != 1) throw new InvalidDataException("Expected exactly one MaterialOverrideData asset.");
                var data = roots[0];
                MaterialOverrideApplicator.Validate(data);
                instance = await OverrideVrmLoader.LoadAsync(data, ct);
                if (!instance) throw new InvalidDataException("The embedded VRM could not be loaded.");
                ct.ThrowIfCancellationRequested();
                var runtime = instance.GetComponent<RuntimeGltfInstance>();
                var lease = MaterialOverrideApplicator.Apply(runtime, data);
                lease.OwnRuntimeResources(runtime);
                lease.OwnBundle(() => Release(path, entry));
                transferred = true;
                runtime.ShowMeshes();
                return new MaterialOverrideModel(instance);
            }
            catch
            {
                if (instance) MaterialOverrideLease.DestroyModelRoot(instance.gameObject);
                throw;
            }
            finally { if (!transferred) Release(path, entry); }
        }

        private static void Release(string path, Entry entry)
        {
            if (--entry.Users != 0) return;
            Bundles.Remove(path);
            if (entry.Bundle) entry.Bundle.Unload(true);
        }
    }
}
