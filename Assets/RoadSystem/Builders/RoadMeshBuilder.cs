using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Roads
{
    public struct RoadMeshData
    {
        public Vector3[] Vertices;
        public int[] Triangles;
        public Vector2[] UVs;
        public Vector3[] Normals;
    }

    public struct RoadColliderData
    {
        public Vector3 Center;
        public Vector3 Size;
        public Quaternion Rotation;
    }

    /// <summary>
    /// Pure mesh/collider generation for a polyline road segment.
    /// </summary>
    public static class RoadMeshBuilder
    {
        public static bool TryBuildMesh(RoadSegment segment, out RoadMeshData meshData)
        {
            meshData = default;

            if (segment == null || segment.ControlPoints == null || segment.ControlPoints.Count < 2)
            {
                return false;
            }

            return TryBuildMesh(segment.ControlPoints, segment.Width, out meshData);
        }

        /// <summary>
        /// Build a mesh for the given control points in local space.
        /// </summary>
        public static bool TryBuildMesh(IReadOnlyList<Vector3> controlPoints, float width, out RoadMeshData meshData)
        {
            meshData = default;

            if (controlPoints == null || controlPoints.Count < 2)
            {
                return false;
            }

            int pointCount = controlPoints.Count;
            var vertices = new Vector3[pointCount * 2];
            var triangles = new int[(pointCount - 1) * 6];
            var uvs = new Vector2[vertices.Length];
            var normals = new Vector3[vertices.Length];

            float halfWidth = width * 0.5f;
            float accumulatedDistance = 0f;

            for (int i = 0; i < pointCount; i++)
            {
                Vector3 point = controlPoints[i];
                Vector3 forward;

                if (i == 0)
                {
                    forward = (controlPoints[i + 1] - point).normalized;
                }
                else if (i == pointCount - 1)
                {
                    forward = (point - controlPoints[i - 1]).normalized;
                }
                else
                {
                    Vector3 prev = controlPoints[i - 1];
                    Vector3 next = controlPoints[i + 1];
                    forward = ((point - prev) + (next - point)).normalized;
                }

                Vector3 left = new Vector3(-forward.z, 0f, forward.x).normalized * halfWidth;
                int vi = i * 2;
                vertices[vi] = point + left;
                vertices[vi + 1] = point - left;

                if (i > 0)
                {
                    accumulatedDistance += Vector3.Distance(controlPoints[i - 1], point);
                }

                uvs[vi] = new Vector2(0f, accumulatedDistance);
                uvs[vi + 1] = new Vector2(1f, accumulatedDistance);
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

        public static List<RoadColliderData> BuildColliders(RoadSegment segment, float height = 0.25f)
        {
            var colliders = new List<RoadColliderData>();

            if (segment == null || segment.ControlPoints == null || segment.ControlPoints.Count < 2)
            {
                return colliders;
            }

            return BuildColliders(segment.ControlPoints, segment.Width, height);
        }

        /// <summary>
        /// Build colliders using the provided control points in local space.
        /// </summary>
        public static List<RoadColliderData> BuildColliders(IReadOnlyList<Vector3> controlPoints, float width, float height = 0.25f)
        {
            var colliders = new List<RoadColliderData>();

            if (controlPoints == null || controlPoints.Count < 2)
            {
                return colliders;
            }

            for (int i = 0; i < controlPoints.Count - 1; i++)
            {
                Vector3 a = controlPoints[i];
                Vector3 b = controlPoints[i + 1];
                Vector3 direction = (b - a).normalized;
                float length = Vector3.Distance(a, b);

                var collider = new RoadColliderData
                {
                    Center = (a + b) * 0.5f,
                    Size = new Vector3(width, height, length),
                    Rotation = Quaternion.LookRotation(direction, Vector3.up)
                };

                // Offset so the collider sits on the ground rather than centered on y
                collider.Center += Vector3.up * (height * 0.5f);
                colliders.Add(collider);
            }

            return colliders;
        }
    }
}
