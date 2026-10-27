using System;
using System.Collections.Generic;
using UnityEngine;

namespace Baku.MaterialOverridableVrm
{
    [Serializable]
    public sealed class RendererMaterialOverrideBinding
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _targetMaterial;
        [SerializeField] private int _materialSlot = -1;
        [SerializeField] private Material _replacementMaterial;
        public Renderer Renderer { get => _renderer; set => _renderer = value; }
        public Material TargetMaterial { get => _targetMaterial; set => _targetMaterial = value; }
        public int MaterialSlot { get => _materialSlot; set => _materialSlot = value; }
        public Material ReplacementMaterial { get => _replacementMaterial; set => _replacementMaterial = value; }
    }

    /// <summary>Export settings only. Never included in the bundle.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VRM/Material Override Source")]
    public sealed class MaterialOverrideSource : MonoBehaviour
    {
        [Tooltip("The original .vrm asset. The original bytes are exported unchanged.")]
        public UnityEngine.Object SourceVrm;
        [Tooltip("Select a renderer, its current material and slot, then the replacement material.")]
        public RendererMaterialOverrideBinding[] RendererBindings = Array.Empty<RendererMaterialOverrideBinding>();

        // Compatibility with the original serialized authoring format. Convert from the Inspector.
        [HideInInspector]
        public MaterialOverrideBinding[] Bindings = Array.Empty<MaterialOverrideBinding>();

#if UNITY_EDITOR
        // Lets the Editor assembly infer asset provenance without an Editor dependency here.
        public static event Action<MaterialOverrideSource> EditorReset;
#endif

        private void Reset()
        {
            RendererBindings = Array.Empty<RendererMaterialOverrideBinding>();
            Bindings = Array.Empty<MaterialOverrideBinding>();
            RefreshRendererBindings();
#if UNITY_EDITOR
            EditorReset?.Invoke(this);
#endif
        }

        /// <summary>Preserves exact renderer/material/slot matches, including duplicate material slots.</summary>
        public void RefreshRendererBindings(bool addMissing = true, bool removeObsolete = true)
        {
            var current = new List<RendererMaterialOverrideBinding>();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                var materials = renderer.sharedMaterials;
                for (var slot = 0; slot < materials.Length; slot++)
                    current.Add(new RendererMaterialOverrideBinding { Renderer = renderer,
                        TargetMaterial = materials[slot], MaterialSlot = slot });
            }
            var result = new List<RendererMaterialOverrideBinding>();
            foreach (var existing in RendererBindings ?? Array.Empty<RendererMaterialOverrideBinding>())
                if (!removeObsolete || current.Exists(item => SameSlot(existing, item))) result.Add(existing);
            if (addMissing)
                foreach (var item in current)
                    if (!result.Exists(existing => SameSlot(existing, item))) result.Add(item);
            RendererBindings = result.ToArray();
        }

        private static bool SameSlot(RendererMaterialOverrideBinding a, RendererMaterialOverrideBinding b) =>
            a != null && b != null && a.Renderer == b.Renderer && a.TargetMaterial == b.TargetMaterial && a.MaterialSlot == b.MaterialSlot;
    }
}
