using UnityEngine;

public class LightingController : MonoBehaviour
{
    [Header("Lighting Settings")]
    public float brightness = 1.38f;
    public float lightMulti = 1.8f;

    private Renderer rend;
    private static readonly int BrightnessID = Shader.PropertyToID("_Brightness");
    private static readonly int LightMultiID = Shader.PropertyToID("_LightMulti");

    void Awake()
    {
        rend = GetComponent<Renderer>();
        ApplyLighting();
    }

    public void ApplyLighting()
    {
        if (rend == null) return;

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        
        rend.GetPropertyBlock(propBlock);
        
        propBlock.SetFloat(BrightnessID, brightness);
        propBlock.SetFloat(LightMultiID, lightMulti);
        
        rend.SetPropertyBlock(propBlock);
    }
}