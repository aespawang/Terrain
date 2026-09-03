// using UnityEditor;
// using UnityEngine;
//
// namespace GaiaTerrain.Editor
// {
//     public class TerrainTextureImporter : AssetPostprocessor
//     {
//         private static readonly TextureImporterPlatformSettings HeightMapImporterPlatformSettings = new()
//         {
//             maxTextureSize = 8192,
//             resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
//             format = TextureImporterFormat.R16
//         };
//         
//         private static readonly TextureImporterPlatformSettings NormalMapImporterPlatformSettings = new()
//         {
//             maxTextureSize = 8192,
//             resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
//             format = TextureImporterFormat.RGB24
//         };
//
//         private static readonly TextureImporterPlatformSettings MinMaxHeightMapImporterPlatformsSettings = new()
//         {
//             maxTextureSize = 2048,
//             resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
//             format = TextureImporterFormat.RGFloat
//         };
//         
//         private void OnPreprocessTexture()
//         {
//             if (!IsInResourcesDir(assetPath)) return;
//
//             var importer = (TextureImporter)assetImporter;
//             var importerSettings = new TextureImporterSettings();
//             if (IsHeightMap(assetPath))
//             {
//                 importer.ReadTextureSettings(importerSettings);
//                 // 只设置必须的
//                 importerSettings.textureType = TextureImporterType.SingleChannel;
//                 importerSettings.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
//                 importerSettings.npotScale = TextureImporterNPOTScale.None;
//                 importerSettings.mipmapEnabled = false;
//                 importerSettings.ignorePngGamma = false;
//                 importerSettings.wrapMode = TextureWrapMode.Clamp;
//                 importerSettings.filterMode = FilterMode.Bilinear;
//                 importer.SetTextureSettings(importerSettings);
//                 HeightMapImporterPlatformSettings.name = importer.GetDefaultPlatformTextureSettings().name;
//                 importer.SetPlatformTextureSettings(HeightMapImporterPlatformSettings);
//                 Debug.Log($"[HeightMap] {assetPath}");
//             }
//             else if (IsNormalMap(assetPath))
//             {
//                 importer.ReadTextureSettings(importerSettings);
//                 importerSettings.textureType = TextureImporterType.Default;
//                 importerSettings.sRGBTexture = false;
//                 importerSettings.npotScale = TextureImporterNPOTScale.None;
//                 importerSettings.mipmapEnabled = false;
//                 importerSettings.ignorePngGamma = false;
//                 importerSettings.wrapMode = TextureWrapMode.Clamp;
//                 importerSettings.filterMode = FilterMode.Bilinear;
//                 importer.SetTextureSettings(importerSettings);
//                 NormalMapImporterPlatformSettings.name = importer.GetDefaultPlatformTextureSettings().name;
//                 importer.SetPlatformTextureSettings(NormalMapImporterPlatformSettings);
//                 Debug.Log($"[NormalMap] {assetPath}");
//             }
//             else if (IsMinMaxHeightMap(assetPath))
//             {
//                 importer.ReadTextureSettings(importerSettings);
//                 importerSettings.textureType = TextureImporterType.Default;
//                 importerSettings.npotScale = TextureImporterNPOTScale.None;
//                 importerSettings.readable = true;
//                 importerSettings.mipmapEnabled = false;
//                 importerSettings.wrapMode = TextureWrapMode.Clamp;
//                 importerSettings.filterMode = FilterMode.Point;
//                 importer.SetTextureSettings(importerSettings);
//                 MinMaxHeightMapImporterPlatformsSettings.name = importer.GetDefaultPlatformTextureSettings().name;
//                 importer.SetPlatformTextureSettings(MinMaxHeightMapImporterPlatformsSettings);
//                 Debug.Log($"[MinMaxHeightMap] {assetPath}");
//             }
//         }
//
//         private static bool IsInResourcesDir(string path)
//         {
//             return path.StartsWith("Assets/Resources/");
//         }
//
//         private static bool IsHeightMap(string path)
//         {
//             var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
//             return path.Contains("HeightMaps") && fileName.StartsWith("HeightMap")
//                 || path.Contains("HeightMapPages") && fileName.StartsWith("HeightMapPage");
//         }
//
//         private static bool IsNormalMap(string path)
//         {
//             var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
//             return path.Contains("NormalMaps") && fileName.StartsWith("NormalMap");
//         }
//         
//         private static bool IsMinMaxHeightMap(string path)
//         {
//             var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
//             return path.Contains("MinMaxHeightMaps") && fileName.StartsWith("MinMaxHeightMap");
//         }
//     }
// }