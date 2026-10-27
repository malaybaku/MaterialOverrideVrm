using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Baku.MaterialOverridableVrm.Editor
{
    internal static class MaterialOverrideEditorLanguage
    {
        internal static bool IsJapaneseEditor(Func<Type> resolve = null)
        {
            try
            {
                var type = resolve != null ? resolve() : AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("UnityEditor.LocalizationDatabase")).FirstOrDefault(t => t != null);
                const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                var enabled = type?.GetProperty("enableEditorLocalization", flags)?.GetValue(null);
                var language = type?.GetProperty("currentEditorLanguage", flags)?.GetValue(null);
                return enabled is bool active && active && language is SystemLanguage value && value == SystemLanguage.Japanese;
            }
            catch { return false; } // Missing/changed internal API: use English.
        }
    }
}
