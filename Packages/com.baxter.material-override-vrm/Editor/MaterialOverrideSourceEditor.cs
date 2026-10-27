using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Baku.MaterialOverridableVrm.Editor
{
    [CustomEditor(typeof(MaterialOverrideSource))]
    public sealed class MaterialOverrideSourceEditor : UnityEditor.Editor
    {
        private bool _converting;
        private readonly Dictionary<int, bool> _rendererFoldouts = new Dictionary<int, bool>();
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SourceVrm"));
            serializedObject.ApplyModifiedProperties();
            var source = (MaterialOverrideSource)target;
            using (new EditorGUI.DisabledScope(_converting || MaterialOverrideMenus.IsExporting))
            {
                if (GUILayout.Button("Reload Bindings")) Refresh(source);
            }
            using (new EditorGUI.DisabledScope(_converting || MaterialOverrideMenus.IsExporting || !source.SourceVrm ||
                EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating))
            {
                if (GUILayout.Button(new GUIContent(MaterialOverrideMenus.IsExporting ? "Exporting..." : "Export...",
                    "Export this component to a .mouv AssetBundle. Choose the destination in the save dialog."))) Export(source);
            }
            serializedObject.Update();
            DrawBindings(serializedObject.FindProperty("RendererBindings"));
            serializedObject.ApplyModifiedProperties();
            if (source.Bindings != null && source.Bindings.Length > 0)
            {
                EditorGUILayout.HelpBox($"Legacy numeric bindings: {source.Bindings.Length}. Convert before editing. Renderer bindings take priority when present.", MessageType.Warning);
                using (new EditorGUI.DisabledScope(_converting || MaterialOverrideMenus.IsExporting || Array.Exists(source.RendererBindings, b => b != null && b.ReplacementMaterial)))
                    if (GUILayout.Button("Convert Legacy Bindings")) Convert(source);
            }
            EditorGUILayout.HelpBox("Leave Override as None to use the material from the original VRM.", MessageType.Info);
        }

        private void DrawBindings(SerializedProperty bindings)
        {
            bindings.isExpanded = EditorGUILayout.Foldout(bindings.isExpanded, $"Bindings ({bindings.arraySize} materials)", true);
            if (!bindings.isExpanded) return;
            if (bindings.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No material slots. Use Reload Bindings after adding renderers.", MessageType.Info);
                return;
            }
            var groups = new Dictionary<int, List<int>>();
            var order = new List<int>();
            for (var i = 0; i < bindings.arraySize; i++)
            {
                var renderer = bindings.GetArrayElementAtIndex(i).FindPropertyRelative("_renderer").objectReferenceValue;
                var key = renderer ? renderer.GetInstanceID() : 0;
                if (!groups.TryGetValue(key, out var indices))
                {
                    indices = new List<int>(); groups.Add(key, indices); order.Add(key);
                }
                indices.Add(i);
            }
            foreach (var key in order)
            {
                var indices = groups[key];
                indices.Sort((a, b) =>
                {
                    var first = bindings.GetArrayElementAtIndex(a).FindPropertyRelative("_materialSlot").intValue;
                    var second = bindings.GetArrayElementAtIndex(b).FindPropertyRelative("_materialSlot").intValue;
                    return first == second ? a.CompareTo(b) : first.CompareTo(second);
                });
                var renderer = bindings.GetArrayElementAtIndex(indices[0]).FindPropertyRelative("_renderer").objectReferenceValue;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (!_rendererFoldouts.TryGetValue(key, out var expanded)) expanded = true;
                    expanded = EditorGUILayout.Foldout(expanded, renderer ? renderer.name : "Missing Renderer", true, EditorStyles.foldoutHeader);
                    _rendererFoldouts[key] = expanded;
                    if (!expanded) continue;
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(bindings.GetArrayElementAtIndex(indices[0]).FindPropertyRelative("_renderer"),
                            new GUIContent("Renderer"));
                    foreach (var index in indices)
                    {
                        var binding = bindings.GetArrayElementAtIndex(index);
                        var slot = binding.FindPropertyRelative("_materialSlot").intValue;
                        EditorGUILayout.PropertyField(binding, new GUIContent($"Material {slot}"), true);
                    }
                }
            }
        }

        private void Refresh(MaterialOverrideSource source)
        {
            Undo.RecordObject(source, "Reload bindings");
            source.RefreshRendererBindings();
            if (PrefabUtility.IsPartOfPrefabInstance(source)) PrefabUtility.RecordPrefabInstancePropertyModifications(source);
            EditorUtility.SetDirty(source);
            serializedObject.Update();
        }

        private async void Export(MaterialOverrideSource source)
        {
            await MaterialOverrideMenus.ExportWithDialogAsync(source);
            if (this) Repaint();
        }

        private async void Convert(MaterialOverrideSource source)
        {
            _converting = true;
            try { await RendererBindingResolver.ConvertLegacyBindingsAsync(source); }
            catch (Exception e) { Debug.LogException(e); }
            finally { _converting = false; if (this) Repaint(); }
        }
    }
}
