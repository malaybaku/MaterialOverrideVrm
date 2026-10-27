using System;
using UnityEngine;

namespace Baku.MaterialOverridableVrm
{
    [Serializable]
    public sealed class MaterialOverrideBinding
    {
        [Min(0)] public int MeshIndex;
        [Min(0)] public int MaterialSlot;
        public Material Material;
    }

    // This is the only explicit entry asset in the AssetBundle.
    public sealed class MaterialOverrideData : ScriptableObject
    {
        public const int CurrentVersion = 1;
        public const string AssetName = "material-override-data.asset";
        public int FormatVersion = CurrentVersion;
        public byte[] VrmBytes;
        public MaterialOverrideBinding[] Bindings = Array.Empty<MaterialOverrideBinding>();
    }
}
