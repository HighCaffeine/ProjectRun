using UnityEngine;

public class MoveState : IState
{
    private Actor actor;
    public MoveState(Actor actor) { this.actor = actor; }

    public void Enter()
    {
        SetMoveAnimation(true);
        SendMoveState();
    }

    public void Execute()
    {
        UpdateMovement();
    }

    public void Exit()
    {
        SetMoveAnimation(false);
        SendStopMovePacket();
    }

    private void SetMoveAnimation(bool isMove)
    {
        actor.animator.SetBool("Move", isMove);
    }

    private void SendMoveState()
    {
        actor.SendStateChange(eState.Move);
    }

    private void UpdateMovement()
    {
        if (!actor.IsLocal) return;
        if (TryReturnToIdle()) return;

        MoveActor();
    }

    private bool TryReturnToIdle()
    {
        if (actor.HasMoveIntent()) return false;

        actor.sm.ChangeState(new IdleState(actor));
        return true;
    }

    private void MoveActor()
    {
        Vector3 moveDir = actor.GetMovementDirection();

        actor.LookAtDirection(moveDir);
        actor.Move(moveDir, actor.MoveSpeed);
    }

    private void SendStopMovePacket()
    {
        actor.SendMovePacket(0f, 0f);
    }
}
