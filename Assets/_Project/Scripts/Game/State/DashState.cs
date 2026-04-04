using UnityEngine;
using static Sentry.MeasurementUnit;

public class DashState : IState
{
    private PlayerActor actor;
    public DashState(PlayerActor actor) { this.actor = actor; }

    private DashCameraEffect dashCameraEffect; // ��� ī�޶� ȿ�� ����
    [SerializeField]
    const float dashSpeedMultiplier = 30; // ��� �ӵ� ����
    [SerializeField]
    const float dashDuration = 0.5f; // ��� ���� �ð�
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

        if (timer >=dashDuration)
        {
            actor.sm.ChangeState(new IdleState(actor));
            return;
        }

        Vector3 moveDir = actor.GetForward().normalized; // ��ô� ���� �ٶ󺸴� �������� �̵�



        float t = Mathf.Clamp01(timer / dashDuration);

        float logValue = Mathf.Log(1 + 3 * t) / Mathf.Log(1 + 3);
        float smooth = logValue * logValue;
       

        actor.Move(moveDir,  smooth * dashSpeedMultiplier);

        actor.sendTimer += Time.deltaTime;
        if (actor.sendTimer >= PlayerActor.sendInterval)
        {
            actor.SendMovePacket(1f,1f);
            actor.sendTimer = 0f;
        }
    }
    public void Exit() { actor.SendMovePacket(0f, 0f); }
}