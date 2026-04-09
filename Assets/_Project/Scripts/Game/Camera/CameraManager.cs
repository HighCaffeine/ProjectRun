using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public interface ICameraEffect
{
    bool IsFinished { get; }
    void Enter(CameraManager manager); // 초기화 및 매니저 참조
    void Execute();                    // Update에서 매 프레임 실행될 로직
    void Exit();                       // 효과 종료 시 정리
}

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    public CinemachineCamera vCam;
    public CinemachineBasicMultiChannelPerlin camPerlin { get; private set; }

    public DashCameraEffect dashCameraEffect;

    private List<ICameraEffect> activeEffects = new List<ICameraEffect>();

    void Awake()
    {
        Instance = this;
        if (vCam != null)
        {
            camPerlin = vCam.GetComponent<CinemachineBasicMultiChannelPerlin>();
            DontDestroyOnLoad(gameObject);
        }
    }

    public void PlayEffect(ICameraEffect effect)
    {
        effect.Enter(this);
        if (!effect.IsFinished)
        {
            activeEffects.Add(effect);
        }
        else
        {
            effect.Exit();
        }
    }

    public void SetupDashEffectComp(PlayerActor localPlayer)
    {
        localPlayer.dashCameraEffect = dashCameraEffect;
    }

    void Update()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            effect.Execute();

            if (effect.IsFinished)
            {
                effect.Exit();
                activeEffects.RemoveAt(i);
            }
        }
    }
}