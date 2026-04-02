using UnityEngine;
using Unity.Cinemachine;

public class CameraShakeEffect : ICameraEffect
{
    public bool IsFinished { get; private set; }

    private CinemachineBasicMultiChannelPerlin perlin;
    private float startingAmplitude;
    private float startingFrequency;
    
    private float duration;
    private float timer;

    public CameraShakeEffect(float amplitude, float frequency, float duration)
    {
        this.startingAmplitude = amplitude;
        this.startingFrequency = frequency;
        this.duration = duration;
        this.timer = duration;
    }

    public void Enter(CameraManager manager)
    {
        this.perlin = manager.camPerlin;
        
        if (perlin != null)
        {
            perlin.AmplitudeGain = startingAmplitude;
            perlin.FrequencyGain = startingFrequency;
        }
        else
        {
            // Perlin 세팅이 안 되어 있으면 즉시 종료 처리
            IsFinished = true; 
        }
    }

    public void Execute()
    {
        if (perlin == null) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            IsFinished = true;
        }
        else
        {
            // 시간이 지남에 따라 진동 세기를 0으로 서서히 줄임 (부드러운 감쇠 효과)
            float progress = timer / duration;
            perlin.AmplitudeGain = Mathf.Lerp(0f, startingAmplitude, progress);
        }
    }

    public void Exit()
    {
        // 쉐이크가 완전히 끝나면 잔진동이 남지 않도록 0으로 확실히 초기화
        if (perlin != null)
        {
            perlin.AmplitudeGain = 0f;
            perlin.FrequencyGain = 0f;
        }
    }
}