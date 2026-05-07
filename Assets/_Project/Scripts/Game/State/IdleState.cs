using System;
using UnityEngine;

public class IdleState : IState
{
    public Actor actor;

    public IdleState(Actor actor)
    {
        this.actor = actor;
    }

    public void Enter()
    {
        actor.SendMovePacket(0f, 0f);
        actor.SendStateChange(eState.Idle);
    }

    public void Execute()
    {
        if (!actor.IsLocal) return;

        if (actor.CheckActionIntent()) return;

        if (actor.HasMoveIntent())
        {
            actor.sm.ChangeState(new MoveState(actor));
        }
    }

    public void Exit()
    {
        //현재 상태에서 초기화 해야 할 것들 idle에서는 따로 x
    }
}