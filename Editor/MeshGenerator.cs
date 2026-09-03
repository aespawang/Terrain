using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GaiaTerrain.Editor
{
    public class MeshGenerator : EditorWindow
    {
        private GUIStyle _paddingBox;
        private int _planeGridCount = 16;
        private float _planeSizePerGrid = 0.5f;
        private string _outputPath = "Assets/";

        [MenuItem("GaiaTerrain/MeshGenerator")]
        private static void CreateWindow()
        {
            GetWindow<MeshGenerator>(nameof(MeshGenerator));
        }

        public void OnEnable()
        {
            _paddingBox = new GUIStyle
            {
                padding = new RectOffset(15, 15, 15, 15)
            };
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(_paddingBox);

            _outputPath = EditorGUILayout.TextField("Export Path", _outputPath);
            _planeGridCount = EditorGUILayout.IntField("Plane Grid Count", _planeGridCount);
            _planeSizePerGrid = EditorGUILayout.FloatField("Plane Size Per Grid", _planeSizePerGrid);

            if (GUILayout.Button("Generate Plane"))
            {
                if (!Directory.Exists(_outputPath))
                {
                    Debug.LogWarning($"{_outputPath} does not exist!");
                }
                else
                {
                    var path = Path.Combine(_outputPath, $"Plane{_planeGridCount}.mesh");
                    GeneratePlaneMesh(path, _planeGridCount, _planeSizePerGrid);
                }
            }

            if (GUILayout.Button("Generate Cube"))
            {
                if (!Directory.Exists(_outputPath))
                {
                    Debug.LogWarning($"{_outputPath} does not exist!");
                }
                else
                {
                    var path = Path.Combine(_outputPath, $"Cube.mesh");
                    GenerateCubeMesh(path);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private static void GeneratePlaneMesh(string outputPath, int planeGridCount, float planeSizePerGrid)
        {
            var mesh = new Mesh();

            var totalMeterSize = planeGridCount * planeSizePerGrid;
            var gridCount = planeGridCount * planeGridCount;
            var triangleCount = gridCount * 2;

            var vOffset = -totalMeterSize * 0.5f;

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var uvStrip = 1f / planeGridCount;
            for (var z = 0; z <= planeGridCount; z++)
            {
                for (var x = 0; x <= planeGridCount; x++)
                {
                    vertices.Add(new Vector3(vOffset + x * 0.5f, 0, vOffset + z * 0.5f));
                    uvs.Add(new Vector2(x * uvStrip, z * uvStrip));
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);

            var indices = new int[triangleCount * 3];

            for (var gridIndex = 0; gridIndex < gridCount; gridIndex++)
            {
                var offset = gridIndex * 6;
                var vIndex = (gridIndex / planeGridCount) * (planeGridCount + 1) + (gridIndex % planeGridCount);

                indices[offset] = vIndex;
                indices[offset + 1] = vIndex + planeGridCount + 1;
                indices[offset + 2] = vIndex + 1;
                indices[offset + 3] = vIndex + 1;
                indices[offset + 4] = vIndex + planeGridCount + 1;
                indices[offset + 5] = vIndex + planeGridCount + 2;
            }

            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.UploadMeshData(false);

            AssetDatabase.CreateAsset(mesh, outputPath);
            AssetDatabase.Refresh();
            Debug.Log($"Created Plane Mesh: {outputPath}");
        }

        private static void GenerateCubeMesh(string outputPath)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            var clonedMesh = Instantiate(mesh);

            AssetDatabase.CreateAsset(clonedMesh, outputPath);
            AssetDatabase.Refresh();
            Debug.Log($"Created Cube Mesh: {outputPath}");

            DestroyImmediate(go);
        }
    }
}