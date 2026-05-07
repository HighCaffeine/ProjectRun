using UnityEngine;

public class MoveState : IState
{
    private Actor actor;
    public MoveState(Actor actor) { this.actor = actor; }

    public void Enter()
    {
        actor.animator.SetBool("Move", true);
        actor.SendStateChange(eState.Move);
    }

    public void Execute()
    {
        if (!actor.IsLocal) return;

        if (actor.CheckActionIntent()) return;

        if (!actor.HasMoveIntent())
        {
            actor.sm.ChangeState(new IdleState(actor));
            return;
        }

        Vector3 moveDir = actor.GetMovementDirection();

        actor.LookAtDirection(moveDir);
        actor.Move(moveDir, actor.MoveSpeed);

        HandleNetworkSync();
    }

    private void HandleNetworkSync()
    {
        actor.sendTimer += Time.deltaTime;
        if (actor.sendTimer >= Actor.sendInterval)
        {
            actor.SendMovePacket(actor.h, actor.v);
            actor.sendTimer = 0f;
        }
    }
    public void Exit()
    {
        actor.animator.SetBool("Move", false);
        actor.SendMovePacket(0f, 0f);
    }
}