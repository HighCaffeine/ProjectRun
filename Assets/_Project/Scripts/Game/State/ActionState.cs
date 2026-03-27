using UnityEngine;

public class ActionState : IState
{
    private PlayerActor actor;
    private byte actionType;
    private float timer;
    private const float CAST_TIME = 0.3f; // 스킬 후딜레이

    public ActionState(PlayerActor actor, byte type)
    {
        this.actor = actor;
        this.actionType = type;
    }

    public void Enter()
    {
    }

    public void Execute()
    {
        // 후딜레이 동안 대기 후 Idle로 복귀
        timer += Time.deltaTime;

        if (timer >= CAST_TIME)
        {
            actor.sm.ChangeState(new IdleState(actor));
        }
    }

    public void Exit() { }
}