using UnityEngine;
using Unity.Cinemachine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private CinemachineBasicMultiChannelPerlin perlin;

    private void Awake()
    {
        perlin = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    public void ShakeCamera(float amplitude, float frequency)
    {
        perlin.AmplitudeGain = amplitude;
        perlin.FrequencyGain = frequency;
    }

    public void ShakeOff()
    {
        perlin.AmplitudeGain = 0.0f;
        perlin.FrequencyGain = 0.0f;
    }
}
