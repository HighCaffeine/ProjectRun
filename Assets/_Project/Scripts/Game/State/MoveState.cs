using UnityEngine;

public class MoveState : IState
{
    private PlayerActor actor;
    public MoveState(PlayerActor actor) { this.actor = actor; }

    public void Enter()
    {
        //actor.SetAni(AniState.Move); 
    }

    public void Execute()
    {
        //밀당 체크
        if (Input.GetMouseButtonDown(0)) { actor.sm.ChangeState(new ActionState(actor, 0)); return; }
        if (Input.GetMouseButtonDown(1)) { actor.sm.ChangeState(new ActionState(actor, 1)); return; }

        // 멈춤 체크
        if (actor.h == 0 && actor.v == 0)
        {
            actor.sm.ChangeState(new IdleState(actor));
            return;
        }

        // 이동 벡터 계산
        Vector3 isoForward = new Vector3(1f, 0f, 1f).normalized;
        Vector3 isoRight = new Vector3(1f, 0f, -1f).normalized;
        Vector3 moveDir = (isoForward * actor.v + isoRight * actor.h).normalized;

        // 회전
        actor.LookAtDirection(moveDir);

        // 이동 및 패킷 전송
        actor.Move(moveDir, actor.moveSpeed);

        actor.sendTimer += Time.deltaTime;
        if (actor.sendTimer >= PlayerActor.sendInterval)
        {
            actor.SendMovePacket(actor.h, actor.v);
            actor.sendTimer = 0f;
        }
    }
    public void Exit() { actor.SendMovePacket(0f, 0f); }
}