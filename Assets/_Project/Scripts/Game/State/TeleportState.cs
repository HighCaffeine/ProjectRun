
using UnityEngine;
public class TeleportState : IState
{
    private PlayerActor actor;
    private Vector3 destPos;

    public TeleportState(PlayerActor actor, Vector3 destPos)
    {
        this.actor = actor;
        this.destPos = destPos;
    }

    public void Enter()
    {
        actor.SetControllerEnabled(false);

        actor.transform.position = destPos;
        Player playerSync = actor.GetComponent<Player>();
        if (playerSync != null) playerSync.SetPos(destPos);
        actor.GetComponent<Player>().SetPos(destPos);

        actor.SetControllerEnabled(true);
        actor.sm.ChangeState(new IdleState(actor));
    }

    public void Execute()
    {
    }

    public void Exit()
    {
    }
}