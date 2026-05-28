using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하나의 평면(Plane)으로 Mesh를 두 개로 자르는 유틸리티.
/// 잘린 단면(캡)을 삼각형 팬으로 채워 닫힌 메쉬를 만듭니다.
/// </summary>
public static class MeshSlicer
{
    // 버텍스 하나를 표현하는 내부 구조체
    private struct VertexData
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    }

    /// <summary>
    /// mesh를 plane으로 잘라 윗조각(positive)과 아랫조각(negative)을 반환합니다.
    /// 실패하면 null 반환.
    /// </summary>
    public static bool Slice(Mesh mesh, Plane plane,
        out Mesh posMesh, out Mesh negMesh)
    {
        posMesh = null;
        negMesh = null;

        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var uvs = mesh.uv;
        var triangles = mesh.triangles;

        // UV가 없을 경우 기본값 채우기
        if (uvs == null || uvs.Length != vertices.Length)
            uvs = new Vector2[vertices.Length];

        // 각 버텍스가 plane의 양쪽 어느 쪽인지 분류
        var sides = new bool[vertices.Length]; // true = positive side
        for (int i = 0; i < vertices.Length; i++)
            sides[i] = plane.GetSide(vertices[i]);

        var posVerts = new List<VertexData>();
        var negVerts = new List<VertexData>();
        var posTris = new List<int>();
        var negTris = new List<int>();

        // 단면(캡) 버텍스 수집용 (순서 있는 루프를 만들기 위해 Dictionary 사용)
        var capVerts = new List<Vector3>(); // 교차점들

        // 각 삼각형을 분류하며 잘린 경우 새 버텍스 생성
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i], i1 = triangles[i + 1], i2 = triangles[i + 2];

            VertexData v0 = MakeVertex(vertices, normals, uvs, i0);
            VertexData v1 = MakeVertex(vertices, normals, uvs, i1);
            VertexData v2 = MakeVertex(vertices, normals, uvs, i2);

            bool s0 = sides[i0], s1 = sides[i1], s2 = sides[i2];
            int posCount = (s0 ? 1 : 0) + (s1 ? 1 : 0) + (s2 ? 1 : 0);

            if (posCount == 3)
            {
                // 전부 positive
                AddTriangle(posVerts, posTris, v0, v1, v2);
            }
            else if (posCount == 0)
            {
                // 전부 negative
                AddTriangle(negVerts, negTris, v0, v1, v2);
            }
            else
            {
                // 평면이 삼각형을 가로지름 → 분할
                SliceTriangle(plane,
                    v0, v1, v2, s0, s1, s2,
                    posVerts, posTris,
                    negVerts, negTris,
                    capVerts);
            }
        }

        // 한쪽에 삼각형이 없으면 슬라이스 실패(평면이 메쉬를 안 자름)
        if (posTris.Count == 0 || negTris.Count == 0)
            return false;

        // 캡(단면) 메쉬 생성 및 양쪽에 추가
        if (capVerts.Count >= 3)
        {
            Vector3 capNormal = plane.normal;
            FillCap(capVerts, capNormal,
                posVerts, posTris,
                negVerts, negTris);
        }

        posMesh = BuildMesh(posVerts, posTris);
        negMesh = BuildMesh(negVerts, negTris);
        return true;
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────

    private static VertexData MakeVertex(Vector3[] v, Vector3[] n, Vector2[] u, int idx)
        => new VertexData
        {
            position = v[idx],
            normal = (n != null && n.Length > idx) ? n[idx] : Vector3.up,
            uv = u[idx]
        };

    private static void AddTriangle(List<VertexData> verts, List<int> tris,
        VertexData a, VertexData b, VertexData c)
    {
        int start = verts.Count;
        verts.Add(a); verts.Add(b); verts.Add(c);
        tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
    }

    /// <summary>
    /// 삼각형을 평면으로 잘라 pos/neg 양쪽에 조각을 추가합니다.
    /// s0/s1/s2 = 각 버텍스가 positive side인지
    /// </summary>
    private static void SliceTriangle(
        Plane plane,
        VertexData v0, VertexData v1, VertexData v2,
        bool s0, bool s1, bool s2,
        List<VertexData> posVerts, List<int> posTris,
        List<VertexData> negVerts, List<int> negTris,
        List<Vector3> capVerts)
    {
        // "홀로 떨어진" 버텍스가 한 쪽에 1개, 나머지 2개가 반대쪽인 경우로 정규화
        // 순서를 맞춰서 항상 v_alone이 pos 혹은 neg 한 쪽에 혼자 있도록 함
        VertexData va, vb, vc;
        bool alone;

        if (s0 == s1) { va = v2; vb = v0; vc = v1; alone = s2; }
        else if (s1 == s2) { va = v0; vb = v1; vc = v2; alone = s0; }
        else { va = v1; vb = v2; vc = v0; alone = s1; }

        // va는 홀로 떨어진 버텍스, vb/vc는 같은 편
        // va-vb 교차점
        float tAB = GetIntersectT(plane, va.position, vb.position);
        VertexData iAB = Lerp(va, vb, tAB);

        // va-vc 교차점
        float tAC = GetIntersectT(plane, va.position, vc.position);
        VertexData iAC = Lerp(va, vc, tAC);

        // 캡에 교차점 추가
        capVerts.Add(iAB.position);
        capVerts.Add(iAC.position);

        if (alone)
        {
            // va가 positive side에 혼자 → pos 쪽에 삼각형 1개, neg 쪽에 쿼드(삼각형 2개)
            AddTriangle(posVerts, posTris, va, iAB, iAC);
            AddTriangle(negVerts, negTris, iAB, vb, vc);
            AddTriangle(negVerts, negTris, iAB, vc, iAC);
        }
        else
        {
            // va가 negative side에 혼자
            AddTriangle(negVerts, negTris, va, iAB, iAC);
            AddTriangle(posVerts, posTris, iAB, vb, vc);
            AddTriangle(posVerts, posTris, iAB, vc, iAC);
        }
    }

    private static float GetIntersectT(Plane plane, Vector3 a, Vector3 b)
    {
        float da = plane.GetDistanceToPoint(a);
        float db = plane.GetDistanceToPoint(b);
        return da / (da - db);
    }

    private static VertexData Lerp(VertexData a, VertexData b, float t) => new VertexData
    {
        position = Vector3.Lerp(a.position, b.position, t),
        normal = Vector3.Lerp(a.normal, b.normal, t).normalized,
        uv = Vector2.Lerp(a.uv, b.uv, t)
    };

    /// <summary>
    /// 캡 버텍스들로 단면을 채웁니다. (컨벡스 폴리곤을 팬 삼각형으로)
    /// pos 쪽에는 normal 방향, neg 쪽에는 반대 방향 노말
    /// </summary>
    private static void FillCap(
        List<Vector3> capVerts,
        Vector3 capNormal,
        List<VertexData> posVerts, List<int> posTris,
        List<VertexData> negVerts, List<int> negTris)
    {
        // 캡 중심점 계산
        Vector3 center = Vector3.zero;
        foreach (var p in capVerts) center += p;
        center /= capVerts.Count;

        // 평면 위 로컬 좌표계 구성
        Vector3 right = Vector3.Cross(capNormal, Vector3.up);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(capNormal, Vector3.forward);
        right.Normalize();
        Vector3 up2 = Vector3.Cross(right, capNormal).normalized;

        // 각도로 정렬 (컨벡스 폴리곤 가정)
        var sorted = new List<Vector3>(capVerts);
        sorted.Sort((a, b) =>
        {
            Vector3 da = a - center, db = b - center;
            float angleA = Mathf.Atan2(Vector3.Dot(da, up2), Vector3.Dot(da, right));
            float angleB = Mathf.Atan2(Vector3.Dot(db, up2), Vector3.Dot(db, right));
            return angleA.CompareTo(angleB);
        });

        // 중복 제거 (교차점이 2개씩 들어오므로)
        var unique = new List<Vector3>();
        foreach (var p in sorted)
        {
            bool dup = false;
            foreach (var q in unique)
                if ((p - q).sqrMagnitude < 0.0001f) { dup = true; break; }
            if (!dup) unique.Add(p);
        }

        if (unique.Count < 3) return;

        VertexData centerPos = new VertexData { position = center, normal = capNormal, uv = Vector2.one * 0.5f };
        VertexData centerNeg = new VertexData { position = center, normal = -capNormal, uv = Vector2.one * 0.5f };

        for (int i = 0; i < unique.Count; i++)
        {
            Vector3 p0 = unique[i];
            Vector3 p1 = unique[(i + 1) % unique.Count];

            Vector2 uv0 = new Vector2(
                0.5f + Vector3.Dot(p0 - center, right),
                0.5f + Vector3.Dot(p0 - center, up2));
            Vector2 uv1 = new Vector2(
                0.5f + Vector3.Dot(p1 - center, right),
                0.5f + Vector3.Dot(p1 - center, up2));

            VertexData vd0Pos = new VertexData { position = p0, normal = capNormal, uv = uv0 };
            VertexData vd1Pos = new VertexData { position = p1, normal = capNormal, uv = uv1 };
            VertexData vd0Neg = new VertexData { position = p0, normal = -capNormal, uv = uv0 };
            VertexData vd1Neg = new VertexData { position = p1, normal = -capNormal, uv = uv1 };

            // pos 쪽: 바깥 방향 노말
            AddTriangle(posVerts, posTris, centerPos, vd0Pos, vd1Pos);
            // neg 쪽: 반전
            AddTriangle(negVerts, negTris, centerNeg, vd1Neg, vd0Neg);
        }
    }

    private static Mesh BuildMesh(List<VertexData> verts, List<int> tris)
    {
        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        var positions = new Vector3[verts.Count];
        var normals = new Vector3[verts.Count];
        var uvs = new Vector2[verts.Count];

        for (int i = 0; i < verts.Count; i++)
        {
            positions[i] = verts[i].position;
            normals[i] = verts[i].normal;
            uvs[i] = verts[i].uv;
        }

        mesh.vertices = positions;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        return mesh;
    }
}