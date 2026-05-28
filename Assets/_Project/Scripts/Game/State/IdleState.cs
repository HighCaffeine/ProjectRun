public class IdleState : IState
{
    public Actor actor;

    public IdleState(Actor actor)
    {
        this.actor = actor;
    }

    public void Enter()
    {
        SendStopMovePacket();
        SendIdleState();
    }

    public void Execute()
    {
        TryChangeToMoveState();
    }

    public void Exit()
    {
        ClearIdleState();
    }

    private void SendStopMovePacket()
    {
        actor.SendMovePacket(0f, 0f);
    }

    private void SendIdleState()
    {
        actor.SendStateChange(eState.Idle);
    }

    private void TryChangeToMoveState()
    {
        if (!actor.IsLocal) return;
        if (!actor.HasMoveIntent()) return;

        actor.sm.ChangeState(new MoveState(actor));
    }

    private void ClearIdleState()
    {
    }
}
