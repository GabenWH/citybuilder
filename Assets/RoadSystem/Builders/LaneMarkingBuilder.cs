using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Roads
{
    public static class LaneMarkingBuilder
    {
        /// <summary>
        /// Builds a thin strip mesh along a lane centerline for visualization.
        /// </summary>
        public static bool TryBuildMarking(IReadOnlyList<Vector3> centerline, float width, out RoadMeshData meshData)
        {
            meshData = default;
            if (centerline == null || centerline.Count < 2)
            {
                return false;
            }

            int pointCount = centerline.Count;
            var vertices = new Vector3[pointCount * 2];
            var triangles = new int[(pointCount - 1) * 6];
            var uvs = new Vector2[vertices.Length];
            var normals = new Vector3[vertices.Length];

            float halfWidth = width * 0.5f;
            float accumulated = 0f;

            for (int i = 0; i < pointCount; i++)
            {
                Vector3 point = centerline[i];
                Vector3 forward;
                if (i == 0)
                {
                    forward = (centerline[i + 1] - point).normalized;
                }
                else if (i == pointCount - 1)
                {
                    forward = (point - centerline[i - 1]).normalized;
                }
                else
                {
                    forward = ((point - centerline[i - 1]) + (centerline[i + 1] - point)).normalized;
                }

                Vector3 left = new Vector3(-forward.z, 0f, forward.x).normalized * halfWidth;
                int vi = i * 2;
                vertices[vi] = point + left;
                vertices[vi + 1] = point - left;

                if (i > 0)
                {
                    accumulated += Vector3.Distance(centerline[i - 1], point);
                }

                uvs[vi] = new Vector2(0f, accumulated);
                uvs[vi + 1] = new Vector2(1f, accumulated);
                normals[vi] = Vector3.up;
                normals[vi + 1] = Vector3.up;

                if (i < pointCount - 1)
                {
                    int triIndex = i * 6;
                    triangles[triIndex] = vi;
                    triangles[triIndex + 1] = vi + 2;
                    triangles[triIndex + 2] = vi + 1;
                    triangles[triIndex + 3] = vi + 1;
                    triangles[triIndex + 4] = vi + 2;
                    triangles[triIndex + 5] = vi + 3;
                }
            }

            meshData = new RoadMeshData
            {
                Vertices = vertices,
                Triangles = triangles,
                UVs = uvs,
                Normals = normals
            };

            return true;
        }
    }
}
