using UnityEngine;
using System.Collections.Generic;

//[ExecuteAlways]
public class OutlineController1 : MonoBehaviour
{
    [Header("Outline Settings")]
    public Material outlineMaterial;

    [Range(0.001f, 0.1f)]
    public float debugWidth = 0.07f;

    public Color debugColor = Color.red;

    private Renderer[] renderers;
    private Dictionary<Renderer, Material[]> originalMats = new Dictionary<Renderer, Material[]>();

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);

        Debug.Log($"[Outline] Renderer ����: {renderers.Length}");

        foreach (var rend in renderers)
        {
            originalMats[rend] = rend.materials;

            Debug.Log($"[Outline] �߰ߵ� Renderer: {rend.name} / Materials: {rend.materials.Length}");
        ApplyOutline(rend);
        }

    }

    // private void OnValidate()
    // {
    //     ApplyOutlineDebug();
    // }

    [ContextMenu("DEBUG: Apply Outline")]
    public void ApplyOutlineDebug()
    {
        if (outlineMaterial == null)
        {
            Debug.LogError("[Outline] Outline Material ����!");
            return;
        }

        outlineMaterial.SetFloat("_OutlineWidth", debugWidth);
        outlineMaterial.SetColor("_OutlineColor", debugColor);

        foreach (var rend in renderers)
        {
            ApplyOutline(rend);
        }
    }

    [ContextMenu("DEBUG: Remove Outline")]
    public void RemoveOutlineDebug()
    {
        foreach (var rend in renderers)
        {
            RestoreOriginal(rend);
        }
    }

    void ApplyOutline(Renderer rend)
    {
        var mats = rend.materials;

        // �α�: ���� ��Ƽ���� ���
        Debug.Log($"[Outline] BEFORE {rend.name} mats:");
        for (int i = 0; i < mats.Length; i++)
        {
            Debug.Log($"  - {i}: {mats[i].name}");
        }

        // �̹� �ִ��� üũ
        foreach (var m in mats)
        {
            if (m == outlineMaterial)
            {
                Debug.Log($"[Outline] �̹� �����: {rend.name}");
                return;
            }
        }

        // �߰�
        Material[] newMats = new Material[mats.Length + 1];
        mats.CopyTo(newMats, 0);
        newMats[newMats.Length - 1] = outlineMaterial;

        rend.materials = newMats;

        // ���� �� �α�
        Debug.Log($"[Outline] AFTER {rend.name} mats:");
        for (int i = 0; i < newMats.Length; i++)
        {
            Debug.Log($"  - {i}: {newMats[i].name}");
        }
    }

    void RestoreOriginal(Renderer rend)
    {
        if (originalMats.ContainsKey(rend))
        {
            rend.materials = originalMats[rend];
            Debug.Log($"[Outline] ������: {rend.name}");
        }
    }
}