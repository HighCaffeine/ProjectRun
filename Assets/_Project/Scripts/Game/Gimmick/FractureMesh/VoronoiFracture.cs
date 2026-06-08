using System.Collections.Generic;
using UnityEngine;

public static class VoronoiFracture
{
    public static List<Mesh> Fracture(Mesh mesh, int siteCount, int seed = -1)
    {
        if (seed < 0)
            seed = System.Environment.TickCount;

        var rng = new System.Random(seed);
        Bounds bounds = mesh.bounds;
        var sites = new Vector3[siteCount];

        for (int i = 0; i < siteCount; i++)
        {
            sites[i] = new Vector3(
                Lerp(bounds.min.x, bounds.max.x, (float)rng.NextDouble()),
                Lerp(bounds.min.y, bounds.max.y, (float)rng.NextDouble()),
                Lerp(bounds.min.z, bounds.max.z, (float)rng.NextDouble()));
        }

        var cells = new List<Mesh>[siteCount];
        for (int i = 0; i < siteCount; i++)
            cells[i] = new List<Mesh> { CopyMesh(mesh) };

        for (int i = 0; i < siteCount; i++)
        {
            for (int j = i + 1; j < siteCount; j++)
            {
                Vector3 mid = (sites[i] + sites[j]) * 0.5f;
                Vector3 normal = (sites[j] - sites[i]).normalized;
                var plane = new Plane(normal, mid);
                bool iIsPositive = plane.GetSide(sites[i]);

                SplitCells(cells[i], plane, iIsPositive);
                SplitCells(cells[j], plane, !iIsPositive);
            }
        }

        var results = new List<Mesh>();
        for (int i = 0; i < siteCount; i++)
        {
            if (cells[i].Count == 0)
                continue;

            Mesh combined = CombineMeshes(cells[i]);
            if (combined != null && combined.vertexCount > 0)
                results.Add(combined);
        }

        return results;
    }

    private static void SplitCells(List<Mesh> meshList, Plane plane, bool keepPositive)
    {
        var newList = new List<Mesh>();

        foreach (Mesh mesh in meshList)
        {
            bool sliced = MeshSlicer.Slice(mesh, plane, out Mesh posMesh, out Mesh negMesh);
            if (sliced)
            {
                newList.Add(keepPositive ? posMesh : negMesh);
                continue;
            }

            if (mesh.vertexCount == 0)
                continue;

            bool onPositive = plane.GetSide(mesh.bounds.center);
            if (onPositive == keepPositive)
                newList.Add(mesh);
        }

        meshList.Clear();
        meshList.AddRange(newList);
    }

    private static Mesh CopyMesh(Mesh src)
    {
        var dst = new Mesh();
        dst.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        dst.vertices = src.vertices;
        dst.normals = src.normals;
        dst.uv = src.uv;
        dst.triangles = src.triangles;
        dst.RecalculateBounds();
        return dst;
    }

    private static Mesh CombineMeshes(List<Mesh> meshes)
    {
        if (meshes.Count == 1)
            return meshes[0];

        var combines = new CombineInstance[meshes.Count];
        for (int i = 0; i < meshes.Count; i++)
        {
            combines[i].mesh = meshes[i];
            combines[i].transform = Matrix4x4.identity;
        }

        var result = new Mesh();
        result.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        result.CombineMeshes(combines, true, false);
        result.RecalculateBounds();
        result.RecalculateNormals();
        return result;
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}