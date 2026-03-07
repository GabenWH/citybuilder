using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityBuilder.Roads
{
    public static class IntersectionMeshBuilder
    {
        /// <summary>
        /// Builds a simple intersection cap at a node by fanning quads from connected segments.
        /// </summary>
        public static bool TryBuild(RoadNode node, RoadNetwork network, float inset, float colliderHeight, float extraRadius, out RoadMeshData meshData, out RoadColliderData colliderData)
        {
            meshData = default;
            colliderData = default;

            if (node == null || network == null)
            {
                return false;
            }

            var connectedSegments = node.Segments
                .Select(id => network.TryGetSegment(id, out var seg) ? seg : null)
                .Where(seg => seg != null)
                .ToList();

            if (connectedSegments.Count < 2)
            {
                return false; // Not an intersection
            }

            var ringLocal = new List<Vector3>();

            foreach (var seg in connectedSegments)
            {
                bool nodeIsStart = seg.StartNodeId == node.Id;
                var points = seg.ControlPoints;
                Vector3 endPoint = nodeIsStart ? points[0] : points[^1];
                Vector3 dir;
                if (points.Count > 1)
                {
                    dir = nodeIsStart ? (points[1] - points[0]).normalized : (points[^2] - points[^1]).normalized;
                }
                else
                {
                    dir = Vector3.forward;
                }

                float halfWidth = seg.Width * 0.5f;
                Vector3 perp = new Vector3(-dir.z, 0f, dir.x).normalized * halfWidth;
                Vector3 along = dir * Mathf.Max(0.01f, inset);

                // Build vertices relative to node center so mesh is local to node
                Vector3 offsetToEnd = endPoint - node.Position;
                ringLocal.Add(offsetToEnd + along + perp);
                ringLocal.Add(offsetToEnd + along - perp);
            }

            // Order by angle so triangles don't cross
            ringLocal = ringLocal.OrderBy(v => Mathf.Atan2(v.z, v.x)).ToList();

            // Ensure winding gives upward normals
            if (ringLocal.Count >= 3)
            {
                Vector3 n = Vector3.Cross(ringLocal[1] - ringLocal[0], ringLocal[2] - ringLocal[0]);
                if (n.y < 0f)
                {
                    ringLocal.Reverse();
                }
            }

            int vertCount = ringLocal.Count + 1; // center + ring
            var vertices = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var normals = new Vector3[vertCount];
            var triangles = new List<int>();

            vertices[0] = Vector3.zero;
            normals[0] = Vector3.up;
            uvs[0] = Vector2.one * 0.5f;

            for (int i = 0; i < ringLocal.Count; i++)
            {
                int vi = i + 1;
                vertices[vi] = ringLocal[i];
                normals[vi] = Vector3.up;

                // Simple planar UVs mapped to 0..1
                Vector3 local = ringLocal[i];
                uvs[vi] = new Vector2(local.x, local.z);
            }

            for (int i = 1; i < vertCount - 1; i++)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);
            }

            // Close the fan
            triangles.Add(0);
            triangles.Add(vertCount - 1);
            triangles.Add(1);

            meshData = new RoadMeshData
            {
                Vertices = vertices,
                Triangles = triangles.ToArray(),
                UVs = uvs,
                Normals = normals
            };

            // Collider covering the patch
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var v in ringLocal)
            {
                bounds.Encapsulate(v);
            }

            const float padding = 0.05f;
            colliderData = new RoadColliderData
            {
                Center = bounds.center + Vector3.up * (colliderHeight * 0.5f),
                Size = new Vector3(bounds.size.x + padding, colliderHeight, bounds.size.z + padding),
                Rotation = Quaternion.identity
            };

            return true;
        }
    }
}
