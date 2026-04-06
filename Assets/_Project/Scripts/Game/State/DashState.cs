using UnityEngine;
using static Sentry.MeasurementUnit;

public class DashState : IState
{
    private PlayerActor actor;
    public DashState(PlayerActor actor) { this.actor = actor; }

    private DashCameraEffect dashCameraEffect; // ï¿½ï¿½ï¿? Ä«ï¿½Þ¶ï¿½ È¿ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½
    [SerializeField]
    const float dashSpeedMultiplier = 30; // ï¿½ï¿½ï¿? ï¿½Óµï¿½ ï¿½ï¿½ï¿½ï¿½
    [SerializeField]
    const float dashDuration = 0.5f; // ï¿½ï¿½ï¿? ï¿½ï¿½ï¿½ï¿½ ï¿½Ã°ï¿½
    private float timer = 0f;
    public void Enter()
    {
        timer = 0f;
        //actor.SetAni(AniState.Dash); 
        dashCameraEffect = actor.dashCameraEffect;
        dashCameraEffect?.OnDash();
    }

    public void Execute()
    {
        timer += Time.deltaTime;

        if (timer >= dashDuration)
        {
            actor.sm.ChangeState(new IdleState(actor));
            return;
        }

        Vector3 moveDir = actor.GetForward().normalized; // ï¿½ï¿½Ã´ï¿? ï¿½ï¿½ï¿½ï¿½ ï¿½Ù¶óº¸´ï¿½ ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ ï¿½Ìµï¿½



        float t = Mathf.Clamp01(timer / dashDuration);

        //  float logValue = Mathf.Log(1 + 3 * t) / Mathf.Log(1 + 3); 1Â÷ ´ë½¬ ÃÊ¹Ý ºü¸§ ÈÄ¹Ý ´À¸²
        //  float smooth = logValue * logValue;

        float smooth = 1 - (t * t);
        actor.Move(moveDir, smooth * dashSpeedMultiplier);

        actor.sendTimer += Time.deltaTime;
        if (actor.sendTimer >= PlayerActor.sendInterval)
        {
            actor.SendMovePacket(1f, 1f);
            actor.sendTimer = 0f;
        }
    }
    public void Exit() { actor.SendMovePacket(0f, 0f); }
}