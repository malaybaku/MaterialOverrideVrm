using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UniGLTF;
using UniVRM10;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Baku.MaterialOverridableVrm.Editor
{
    public static class MaterialOverrideExporter
    {
        public static async Task ExportAsync(MaterialOverrideSource source, string outputPath)
        {
            if (!source) throw new ArgumentNullException(nameof(source));
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Export is only available in Edit Mode.");
            if (!MaterialOverrideRenderPipeline.IsUniversal(MaterialOverrideRenderPipeline.ConfiguredPipeline))
                throw new InvalidOperationException("Export requires an active Universal Render Pipeline asset.");
            var sourcePath = AssetDatabase.GetAssetPath(source.SourceVrm);
            if (!string.Equals(Path.GetExtension(sourcePath), ".vrm", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Select the original .vrm asset in Source VRM.");
            var data = ScriptableObject.CreateInstance<MaterialOverrideData>();
            var tempFolder = "Assets/__MaterialOverrideExport_" + Guid.NewGuid().ToString("N");
            var buildFolder = Path.GetFullPath("Library/MaterialOverrideBuild/" + Guid.NewGuid().ToString("N"));
            // Unity identifies loaded bundles by their internal archive name, not the output path.
            // Separate exports must be loadable together, even when their source assets are identical.
            var bundleName = "model_" + Guid.NewGuid().ToString("N") + ".mouv";
            Vrm10Instance validationInstance = null;
            try
            {
                data.VrmBytes = File.ReadAllBytes(sourcePath);
                validationInstance = await Vrm10.LoadBytesAsync(data.VrmBytes, canLoadVrm0X: false,
                    showMeshes: false, awaitCaller: new ImmediateCaller());
                if (!validationInstance) throw new ArgumentException("Source must be a VRM 1.0 model.");
                data.Bindings = RendererBindingResolver.Resolve(source, data.VrmBytes, validationInstance.GetComponent<RuntimeGltfInstance>());
                MaterialOverrideApplicator.Validate(data);
                foreach (var binding in data.Bindings)
                    if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(binding.Material)))
                        throw new ArgumentException("Override materials must be saved project assets.");
                using (var lease = MaterialOverrideApplicator.Apply(validationInstance.GetComponent<RuntimeGltfInstance>(), data)) { }

                AssetDatabase.CreateFolder("Assets", Path.GetFileName(tempFolder));
                var assetPath = tempFolder + "/" + MaterialOverrideData.AssetName;
                AssetDatabase.CreateAsset(data, assetPath);
                AssetDatabase.SaveAssets();
                foreach (var dependency in AssetDatabase.GetDependencies(assetPath, true))
                {
                    var type = AssetDatabase.GetMainAssetTypeAtPath(dependency);
                    if (type != null && (typeof(GameObject).IsAssignableFrom(type) || typeof(Component).IsAssignableFrom(type)
                        || typeof(Mesh).IsAssignableFrom(type) || typeof(Avatar).IsAssignableFrom(type)))
                        throw new InvalidOperationException($"Unexpected model dependency in bundle: {dependency}");
                }
                Directory.CreateDirectory(buildFolder);
                AssetBundleManifest manifest;
                using (new MaterialOverrideShaderVariants())
                manifest = BuildPipeline.BuildAssetBundles(buildFolder, new[]
                {
                    new AssetBundleBuild { assetBundleName = bundleName, assetNames = new[] { assetPath },
                        addressableNames = new[] { MaterialOverrideData.AssetName } }
                }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                    EditorUserBuildSettings.activeBuildTarget);
                if (!manifest) throw new IOException("AssetBundle build failed. See the Unity Console.");
                outputPath = Path.GetFullPath(outputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.Copy(Path.Combine(buildFolder, bundleName), outputPath, true);
            }
            finally
            {
                if (validationInstance)
                {
                    validationInstance.GetComponent<RuntimeGltfInstance>()
                        .TransferOwnership((key, value) => UnityEngine.Object.DestroyImmediate(value));
                    UnityEngine.Object.DestroyImmediate(validationInstance.gameObject);
                }
                if (AssetDatabase.IsValidFolder(tempFolder)) AssetDatabase.DeleteAsset(tempFolder);
                if (data) UnityEngine.Object.DestroyImmediate(data);
                // Generated build files are retained under Library for diagnostics.
            }
        }
    }
}
