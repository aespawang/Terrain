using System.IO;
using UnityEditor;
using UnityEngine;

namespace GaiaTerrain.Editor
{
    public class DemoImportSettingWindow : EditorWindow
    {
        private string _assetsPath = "Assets/TerrainDemoScene_URP";

        [MenuItem("GaiaTerrain/DemoImportSettingWindow")]
        private static void CreateWindow()
        {
            GetWindow<DemoImportSettingWindow>(nameof(DemoImportSettingWindow));
        }

        private void OnGUI()
        {
            _assetsPath = EditorGUILayout.TextField("Assets Path", _assetsPath);

            if (GUILayout.Button("Apply"))
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (string.IsNullOrEmpty(_assetsPath) || !Directory.Exists(_assetsPath))
            {
                EditorUtility.DisplayDialog("Error", "Invalid assets path!", "OK");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Path.Combine(_assetsPath, "Terrain", "Textures") });

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (!importer) continue;
            
                var fileName = Path.GetFileNameWithoutExtension(assetPath);

                if (fileName.EndsWith("_BaseColor") || fileName.EndsWith("_Albedo"))
                {
                    ApplyBaseColorMapSettings(importer);
                    Debug.Log($"Applied BaseColorMap: {assetPath}");
                }
                else if (fileName.EndsWith("_Normal"))
                {
                    // TODO
                }
                else if (fileName.EndsWith("_MaskMap"))
                {
                    // TODO
                }
            }
        }

        private static void ApplyBaseColorMapSettings(TextureImporter importer)
        {
            if (!importer) return;
            
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.swizzleA = TextureImporterSwizzle.Zero;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
    }
}