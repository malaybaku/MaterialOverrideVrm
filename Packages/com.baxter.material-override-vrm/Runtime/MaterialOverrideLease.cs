using System;
using System.Collections.Generic;
using UnityEngine;
using UniGLTF;

namespace Baku.MaterialOverridableVrm
{
    /// <summary>Owns per-instance override materials. Disposal clears their slots; it does not restore originals.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class MaterialOverrideLease : MonoBehaviour, IDisposable
    {
        private List<Renderer> _renderers;
        private List<Material> _materials;
        private Action _releaseBundle;
        private RuntimeGltfInstance _runtimeOwner;
        private bool _disposed;

        internal void Initialize(List<Renderer> renderers, List<Material> materials)
        {
            _renderers = renderers;
            _materials = materials;
        }

        internal void OwnBundle(Action release) => _releaseBundle = release;
        internal void OwnRuntimeResources(RuntimeGltfInstance runtime) => _runtimeOwner = runtime;

        public void Dispose()
        {
            Cleanup();
            if (this) DestroyObject(this);
        }

        private void OnDestroy() => Cleanup();

        private void Cleanup()
        {
            if (_disposed) return;
            _disposed = true;
            if (_renderers != null && _materials != null)
                foreach (var renderer in _renderers)
                {
                    if (!renderer) continue;
                    var slots = renderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                        if (_materials.Contains(slots[i])) slots[i] = null;
                    renderer.sharedMaterials = slots;
                }
            if (_materials != null)
                foreach (var material in _materials) DestroyObject(material);
            // RuntimeGltfInstance.OnDestroy is not guaranteed in Edit Mode. Taking ownership here also
            // removes its entries, so a later UniVRM OnDestroy cannot destroy the same resource twice.
            // The managed resource list is usable even if Unity has already destroyed that component.
            if (!ReferenceEquals(_runtimeOwner, null))
                _runtimeOwner.TransferOwnership((key, value) => DestroyObject(value));
            _runtimeOwner = null;
            _releaseBundle?.Invoke();
            _releaseBundle = null;
        }

        internal new static void DestroyObject(UnityEngine.Object value)
        {
            if (!value) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        internal static void DestroyModelRoot(GameObject root)
        {
            if (!root) return;
            var runtime = root.GetComponent<RuntimeGltfInstance>();
            if (runtime) runtime.TransferOwnership((key, value) => DestroyObject(value));
            DestroyObject(root);
        }
    }
}
