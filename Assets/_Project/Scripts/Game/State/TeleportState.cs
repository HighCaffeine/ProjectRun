using UnityEngine;

public class TeleportState : IState
{
    private Actor actor;
    private Vector3 destPos;

    public TeleportState(Actor actor, Vector3 destPos)
    {
        this.actor = actor;
        this.destPos = destPos;
    }

    public void Enter()
    {
        DisableController();
        ResetActorVerticalVelocity();
        MoveActorToDestination();
        SyncPlayerPosition();
        EnableController();
        ReturnToIdle();
    }

    public void Execute()
    {
        UpdateTeleport();
    }

    public void Exit()
    {
        CompleteTeleport();
    }

    private void DisableController()
    {
        actor.SetControllerActive(false);
    }

    private void ResetActorVerticalVelocity()
    {
        actor.ResetVerticalVelocity();
    }

    private void MoveActorToDestination()
    {
        actor.transform.position = destPos;
    }

    private void SyncPlayerPosition()
    {
        Player playerSync = actor.GetComponent<Player>();
        if (playerSync != null) playerSync.SetPos(destPos);
    }

    private void EnableController()
    {
        actor.SetControllerActive(true);
    }

    private void ReturnToIdle()
    {
        actor.sm.ChangeState(new IdleState(actor));
    }

    private void UpdateTeleport()
    {
    }

    private void CompleteTeleport()
    {
    }
}
