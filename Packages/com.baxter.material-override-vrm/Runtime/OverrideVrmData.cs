using System;
using System.IO;
using System.Runtime.CompilerServices;
using UniGLTF;
using UniVRM10;

[assembly: InternalsVisibleTo("MaterialOverridableVrm.Editor")]

namespace Baku.MaterialOverridableVrm
{
    /// <summary>Owns the parsed import layout, including VRM 0.x migration. Never changes the stored bytes.</summary>
    internal sealed class OverrideVrmData : IDisposable
    {
        private readonly GltfData _original;
        private readonly GltfData _migrated;
        internal Vrm10Data Vrm { get; }
        internal GltfData Data => Vrm.Data;

        internal OverrideVrmData(byte[] bytes)
        {
            // Migration can modify accessor buffers while rotating the VRM 0.x model.
            _original = new GlbLowLevelParser("", (byte[])bytes.Clone()).Parse();
            try
            {
                Vrm = Vrm10Data.Parse(_original);
                if (Vrm != null) return;
                _migrated = Vrm10Data.Migrate(_original, out var vrm, out var migration);
                Vrm = vrm;
                if (Vrm == null)
                    throw new InvalidDataException("Expected a VRM 1.0 or migratable VRM 0.x model. " + migration?.Message);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            _migrated?.Dispose();
            _original.Dispose();
        }
    }
}
