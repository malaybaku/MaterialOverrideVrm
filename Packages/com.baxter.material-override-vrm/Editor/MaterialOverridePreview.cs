using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Baku.MaterialOverridableVrm.Editor
{
    // State belongs to the Editor, never to a scene component or an asset.
    [InitializeOnLoad]
    internal static class MaterialOverridePreview
    {
        private sealed class Session
        {
            internal readonly Dictionary<Renderer, Material[]> Originals = new Dictionary<Renderer, Material[]>();
            internal readonly Dictionary<Material, Material> Clones = new Dictionary<Material, Material>();

            internal void Restore()
            {
                foreach (var pair in Originals)
                    if (pair.Key) pair.Key.sharedMaterials = pair.Value;
                foreach (var clone in Clones.Values)
                    if (clone) UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static readonly Dictionary<MaterialOverrideSource, Session> Sessions = new Dictionary<MaterialOverrideSource, Session>();

        static MaterialOverridePreview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
                    StopAll();
            };
            AssemblyReloadEvents.beforeAssemblyReload += StopAll;
            EditorApplication.quitting += StopAll;
            EditorApplication.update += RemoveDestroyedSources;
        }

        internal static bool IsActive(MaterialOverrideSource source) => source && Sessions.ContainsKey(source);

        internal static void Start(MaterialOverrideSource source)
        {
            if (!EditorApplication.isPlaying || !Application.IsPlaying(source) || EditorUtility.IsPersistent(source))
                throw new InvalidOperationException("Preview requires a scene instance in Play Mode.");
            if (IsActive(source)) throw new InvalidOperationException("Preview is already active.");
            if (source.RendererBindings == null || source.RendererBindings.Length == 0)
                throw new InvalidOperationException("No renderer bindings. Reload or convert legacy bindings before previewing.");

            var replacements = new Dictionary<Renderer, Material[]>();
            var slots = new HashSet<(Renderer, int)>();
            // Validate the entire selection before allocating materials or changing renderers.
            foreach (var binding in source.RendererBindings)
            {
                if (binding == null) throw new ArgumentException("A binding is missing. Reload Bindings first.");
                if (!binding.ReplacementMaterial) continue;
                var renderer = binding.Renderer;
                if (!renderer || !renderer.transform.IsChildOf(source.transform))
                    throw new ArgumentException("Each override renderer must belong to this model.");
                foreach (var session in Sessions.Values)
                    if (session.Originals.ContainsKey(renderer))
                        throw new InvalidOperationException($"Renderer '{renderer.name}' is already being previewed.");
                var materials = renderer.sharedMaterials;
                // UniVRM replaces prefab materials with expression-owned instances on entering
                // Play Mode. The authored target asset therefore cannot be compared by identity.
                if (binding.MaterialSlot < 0 || binding.MaterialSlot >= materials.Length)
                    throw new ArgumentException($"The selected slot on '{renderer.name}' no longer exists. Reload Bindings first.");
                if (!slots.Add((renderer, binding.MaterialSlot)))
                    throw new ArgumentException($"Duplicate override slot on '{renderer.name}'.");
                if (!replacements.ContainsKey(renderer)) replacements.Add(renderer, materials);
                replacements[renderer][binding.MaterialSlot] = binding.ReplacementMaterial;
            }
            if (replacements.Count == 0) throw new InvalidOperationException("Assign at least one Override material before previewing.");

            var preview = new Session();
            try
            {
                foreach (var pair in replacements) preview.Originals.Add(pair.Key, pair.Key.sharedMaterials);
                foreach (var slot in slots)
                {
                    var material = replacements[slot.Item1][slot.Item2];
                    if (!preview.Clones.TryGetValue(material, out var clone))
                    {
                        clone = new Material(material) { hideFlags = HideFlags.HideAndDontSave };
                        preview.Clones.Add(material, clone);
                    }
                    replacements[slot.Item1][slot.Item2] = clone;
                }
                foreach (var pair in replacements) pair.Key.sharedMaterials = pair.Value;
                Sessions.Add(source, preview);
            }
            catch
            {
                preview.Restore();
                throw;
            }
            SceneView.RepaintAll();
        }

        internal static void Stop(MaterialOverrideSource source)
        {
            if (!Sessions.TryGetValue(source, out var preview)) return;
            Sessions.Remove(source);
            preview.Restore();
            SceneView.RepaintAll();
        }

        private static void StopAll()
        {
            foreach (var preview in Sessions.Values) preview.Restore();
            Sessions.Clear();
        }

        private static void RemoveDestroyedSources()
        {
            if (Sessions.Count == 0) return;
            var removed = new List<MaterialOverrideSource>();
            foreach (var pair in Sessions)
                if (!pair.Key) removed.Add(pair.Key);
            foreach (var source in removed) Stop(source);
        }
    }
}
