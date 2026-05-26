using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FractureObject : MonoBehaviour
{
    private BaseGimmick gimmick;

    [SerializeField]
    [Range(2, 50)]
    private int siteCount = 15;

    [SerializeField]
    private int seed = -1;
    [SerializeField]
    private bool surfaceOnly = false;
    [SerializeField]
    private float shellThickness = 0.05f;
    [SerializeField]
    private bool addBackFaces = true;

    [SerializeField]
    private float breakForce = 10f;
    [SerializeField]
    private float density = 1f;
    [SerializeField]
    private float minChunkMass = 0.2f;
    [SerializeField]
    private float destroyDelay = 5f;
    [SerializeField]
    private float explosionImpulse = 8f;
    [SerializeField]
    private float randomImpulse = 3f;
    [SerializeField]
    private float torqueImpulse = 2f;
    [SerializeField]
    private float directionalImpulse = 8f;
    [SerializeField]
    private float directionalRandomImpulse = 3f;
    [SerializeField]
    private float directionalUpwardImpulse = 1f;

    [SerializeField]
    private Material outsideMaterial;
    [SerializeField]
    private Material insideMaterial;
    [SerializeField]
    private bool breakAfterStartForTest = false;
    [SerializeField]
    private float testBreakDelay = 3f;
    [SerializeField]
    private Vector3 testBreakDirection = Vector3.left;


    private bool isBroken = false;
    private GameObject chunksRoot;
    private MeshRenderer renderer;
    private Collider collider;


    private void Awake()
    {
        gimmick = GetComponent<BaseGimmick>();

        renderer = gimmick.TargetTransform.GetComponentInChildren<MeshRenderer>();
        collider = gimmick.TargetTransform.GetComponentInChildren<Collider>();
    }

    private void Start()
    {
        PrepareFracture();

        if (breakAfterStartForTest)
        {
            StartCoroutine(BreakAfterDelayForTest());
        }
    }

    public void Break(Vector3 impactVelocity = default)
    {
        BreakInternal(impactVelocity, Vector3.zero, false);
    }

    public void BreakToDirection(Vector3 launchDirection, Vector3 impactVelocity = default)
    {
        BreakInternal(impactVelocity, launchDirection, true);
    }

    private void BreakInternal(Vector3 impactVelocity, Vector3 launchDirection, bool useDirection)
    {
        if (isBroken) return;
        isBroken = true;

        if (renderer != null) renderer.enabled = false;
        if (collider != null) collider.enabled = false;

        if (chunksRoot != null)
        {
            chunksRoot.transform.SetParent(null, true);
            chunksRoot.SetActive(true);
            Debug.Log("FractureObject break");

            if (useDirection)
            {
                ExplodeChunksToDirection(launchDirection, impactVelocity);
            }
            else
            {
                ExplodeChunksFromCenter(impactVelocity);
            }

            if (destroyDelay > 0f)
            {
                StartCoroutine(DestroyChunks());
            }
        }
    }

    private void PrepareFracture()
    {
        var mf = gimmick.TargetTransform.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            return;
        }

        Mesh localMesh = ToLocalMesh(mf.sharedMesh);

        // Voronoi 파쇄
        List<Mesh> chunks = VoronoiFracture.Fracture(localMesh, siteCount, seed);

        if (surfaceOnly) chunks = KeepOuterShellChunks(chunks, localMesh.bounds, shellThickness, addBackFaces);

        if (chunks.Count == 0)
        {
            return;
        }

        Material outerMat = outsideMaterial != null
            ? outsideMaterial
            : renderer.sharedMaterial;
        Material innerMat = insideMaterial != null
            ? insideMaterial
            : new Material(Shader.Find("Standard"));

        // 조각 부모 오브젝트
        chunksRoot = new GameObject($"{name}_Chunks");
        chunksRoot.transform.SetParent(transform, false);
        chunksRoot.transform.localPosition = Vector3.zero;
        chunksRoot.transform.localRotation = Quaternion.identity;
        chunksRoot.transform.localScale = gimmick.TargetTransform.localScale;

        float worldVolumeScale = GetWorldVolumeScale();

        for (int i = 0; i < chunks.Count; i++)
        {
            Mesh chunk = chunks[i];
            chunk.name = $"{name}_Chunk_{i}";

            var obj = new GameObject($"Chunk_{i}");
            obj.transform.SetParent(chunksRoot.transform, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            obj.AddComponent<MeshFilter>().mesh = chunk;
            obj.AddComponent<MeshRenderer>().materials = new[] { outerMat, innerMat };

            var rigde = obj.AddComponent<Rigidbody>();
            rigde.mass = Mathf.Max(minChunkMass, MeshVolume(chunk) * worldVolumeScale * density);

            var mc = obj.AddComponent<MeshCollider>();
            mc.sharedMesh = chunk;
            mc.convex = true;

            var joint = obj.AddComponent<FixedJoint>();
            joint.breakForce = breakForce;
            joint.breakTorque = breakForce;
        }

        chunksRoot.SetActive(false);
    }

    private static List<Mesh> KeepOuterShellChunks(List<Mesh> chunks, Bounds sourceBounds, float thickness, bool addBackFaces)
    {
        var result = new List<Mesh>();
        float safeThickness = Mathf.Max(0.0001f, thickness);

        foreach (var chunk in chunks)
        {
            Mesh shell = KeepOuterShell(chunk, sourceBounds, safeThickness, addBackFaces);
            if (shell != null && shell.vertexCount > 0 && shell.triangles.Length > 0) result.Add(shell);
        }

        return result;
    }

    private static Mesh KeepOuterShell(Mesh mesh, Bounds sourceBounds, float thickness, bool addBackFaces)
    {
        var srcVertices = mesh.vertices;
        var srcNormals = mesh.normals;
        var srcUvs = mesh.uv;
        var srcTriangles = mesh.triangles;

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        for (int i = 0; i < srcTriangles.Length; i += 3)
        {
            int i0 = srcTriangles[i];
            int i1 = srcTriangles[i + 1];
            int i2 = srcTriangles[i + 2];
            Vector3 center = (srcVertices[i0] + srcVertices[i1] + srcVertices[i2]) / 3f;

            if (!IsNearBoundsSurface(center, sourceBounds, thickness)) continue;

            int start = vertices.Count;
            AddCopiedVertex(i0, srcVertices, srcNormals, srcUvs, vertices, normals, uvs, false);
            AddCopiedVertex(i1, srcVertices, srcNormals, srcUvs, vertices, normals, uvs, false);
            AddCopiedVertex(i2, srcVertices, srcNormals, srcUvs, vertices, normals, uvs, false);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);

            if (addBackFaces)
            {
                int backStart = vertices.Count;
                AddCopiedVertex(i2, srcVertices, srcNormals, srcUvs, vertices, normals, uvs, true);
                AddCopiedVertex(i1, srcVertices, srcNormals, srcUvs, vertices, normals, uvs, true);
                AddCopiedVertex(i0, srcVertices, srcNormals, srcUvs, vertices, normals, uvs, true);
                triangles.Add(backStart);
                triangles.Add(backStart + 1);
                triangles.Add(backStart + 2);
            }
        }

        if (triangles.Count == 0)
        {
            return null;
        }

        var shell = new Mesh();
        shell.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        shell.vertices = vertices.ToArray();
        shell.normals = normals.ToArray();
        shell.uv = uvs.ToArray();
        shell.triangles = triangles.ToArray();
        shell.RecalculateBounds();
        shell.RecalculateNormals();
        return shell;
    }

    private static bool IsNearBoundsSurface(Vector3 point, Bounds bounds, float thickness)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        float distanceToSurface = Mathf.Min(
            Mathf.Abs(point.x - min.x), Mathf.Abs(max.x - point.x),
            Mathf.Abs(point.y - min.y), Mathf.Abs(max.y - point.y),
            Mathf.Abs(point.z - min.z), Mathf.Abs(max.z - point.z));

        return distanceToSurface <= thickness;
    }

    private static void AddCopiedVertex(int index, Vector3[] srcVertices, Vector3[] srcNormals, Vector2[] srcUvs, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, bool flipNormal)
    {
        Vector3 normal = srcNormals != null && srcNormals.Length > index ? srcNormals[index] : Vector3.up;
        vertices.Add(srcVertices[index]);
        normals.Add(flipNormal ? -normal : normal);
        uvs.Add(srcUvs != null && srcUvs.Length > index ? srcUvs[index] : Vector2.zero);
    }
    private void ExplodeChunksFromCenter(Vector3 impactVelocity)
    {
        if (chunksRoot == null)
        {
            return;
        }
        Vector3 center = transform.position;
        foreach (var rig in chunksRoot.GetComponentsInChildren<Rigidbody>())
        {
            foreach (var joint in rig.GetComponents<Joint>())
            {
                Destroy(joint);
            }
            rig.isKinematic = false;
            rig.WakeUp();

            Vector3 outward = rig.worldCenterOfMass - center;
            if (outward.sqrMagnitude < 0.0001f)
            {
                outward = Random.onUnitSphere;
            }
            outward.Normalize();

            Vector3 randomDirection = Random.onUnitSphere;
            Vector3 impulse = (outward * explosionImpulse + randomDirection * randomImpulse) * rig.mass;
            if (impactVelocity != Vector3.zero)
            {
                impulse += impactVelocity * 0.5f;
            }
            rig.AddForce(impulse, ForceMode.Impulse);
            rig.AddTorque(Random.onUnitSphere * torqueImpulse * rig.mass, ForceMode.Impulse);
        }
    }
    private void ExplodeChunksToDirection(Vector3 launchDirection, Vector3 impactVelocity)
    {
        if (chunksRoot == null)
        {
            return;
        }

        launchDirection.y = 0f;
        if (launchDirection.sqrMagnitude < 0.0001f)
        {
            launchDirection = transform.forward;
        }
        launchDirection.Normalize();

        foreach (var rig in chunksRoot.GetComponentsInChildren<Rigidbody>())
        {
            foreach (var joint in rig.GetComponents<Joint>())
            {
                Destroy(joint);
            }

            rig.isKinematic = false;
            rig.WakeUp();

            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = Mathf.Abs(randomDirection.y);

            Vector3 impulse = launchDirection * directionalImpulse
                + randomDirection * directionalRandomImpulse
                + Vector3.up * directionalUpwardImpulse;

            if (impactVelocity != Vector3.zero)
            {
                impulse += impactVelocity * 0.5f;
            }

            rig.AddForce(impulse * rig.mass, ForceMode.Impulse);
            rig.AddTorque(Random.onUnitSphere * torqueImpulse * rig.mass, ForceMode.Impulse);
        }
    }
    private Mesh ToLocalMesh(Mesh src)
    {
        var dst = new Mesh();
        dst.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        var verts = src.vertices;

        dst.vertices = verts;
        dst.normals = src.normals;
        dst.uv = src.uv;
        dst.triangles = src.triangles;
        dst.RecalculateBounds();
        return dst;
    }

    private static float MeshVolume(Mesh mesh)
    {
        float vol = 0f;
        var verts = mesh.vertices;
        var tris = mesh.triangles;

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 p1 = verts[tris[i]];
            Vector3 p2 = verts[tris[i + 1]];
            Vector3 p3 = verts[tris[i + 2]];
            vol += Vector3.Dot(p1, Vector3.Cross(p2, p3)) / 6f;
        }
        return Mathf.Abs(vol);
    }

    private float GetWorldVolumeScale()
    {
        Vector3 scale = transform.lossyScale;
        return Mathf.Abs(scale.x * scale.y * scale.z);
    }

    private IEnumerator DestroyChunks()
    {
        yield return new WaitForSeconds(destroyDelay);
        if (chunksRoot != null)
        {
            Destroy(chunksRoot);
        }
    }

    private IEnumerator BreakAfterDelayForTest()
    {
        yield return new WaitForSeconds(testBreakDelay);
        BreakToDirection(testBreakDirection);
    }

}
