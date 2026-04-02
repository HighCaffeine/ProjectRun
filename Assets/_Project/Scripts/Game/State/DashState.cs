using UnityEngine;

public class DashState : IState
{
    private PlayerActor actor;
    public DashState(PlayerActor actor) { this.actor = actor; }

    [SerializeField]
    const float dashSpeedMultiplier = 3f; // 대시 속도 배율
    [SerializeField]
    const float dashDuration = 0.5f; // 대시 지속 시간
    private float timer = 0f;
    public void Enter()
    {
        timer = 0f;
        //actor.SetAni(AniState.Dash); 
    }

    public void Execute()
    {
        timer += Time.deltaTime;

        if (timer >=dashDuration)
        {
            actor.sm.ChangeState(new IdleState(actor));
            return;
        }

        Vector3 moveDir = actor.GetForward().normalized; // 대시는 현재 바라보는 방향으로 이동


        // 이동 및 패킷 전송
        actor.Move(moveDir, actor.moveSpeed * dashSpeedMultiplier);

        actor.sendTimer += Time.deltaTime;
        if (actor.sendTimer >= PlayerActor.sendInterval)
        {
            actor.SendMovePacket(1f,1f);
            actor.sendTimer = 0f;
        }
    }
    public void Exit() { actor.SendMovePacket(0f, 0f); }
}