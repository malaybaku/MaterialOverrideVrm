using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Baku.MaterialOverridableVrm.Editor
{
    [CustomPropertyDrawer(typeof(RendererMaterialOverrideBinding))]
    public sealed class RendererMaterialOverrideBindingDrawer : PropertyDrawer
    {
        private static float Row => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => Row * 3;

        public static int[] GetMatchingSlots(Renderer renderer, Material material)
        {
            var result = new List<int>();
            if (renderer)
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++) if (materials[i] == material) result.Add(i);
            }
            return result.ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                rect.y += Row;
                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative("_targetMaterial"), new GUIContent("Current"));
                rect.y += Row;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("_replacementMaterial"),
                    new GUIContent("Override", "None keeps the current material and excludes this row from export."));
            }
            EditorGUI.EndProperty();
        }
    }
}
