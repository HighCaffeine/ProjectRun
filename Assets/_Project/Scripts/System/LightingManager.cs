using UnityEngine;

public class LightingManager : MonoBehaviour
{
    public float sceneBrightness = 1.0f;
    public float sceneLightMulti = 1.0f;

    void Start()
    {
        var controllers = FindObjectsByType<LightingController>(FindObjectsSortMode.None);
        foreach (var ctrl in controllers)
        {
            ctrl.brightness = sceneBrightness;
            ctrl.lightMulti = sceneLightMulti;
            ctrl.ApplyLighting();
        }
    }
}