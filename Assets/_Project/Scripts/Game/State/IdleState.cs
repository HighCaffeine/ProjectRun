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
        //여기서 idle 애니메이션 실행
        /*
        공격을 예로 들면
        여기서 움직임을 잠궈주고 무기에 충돌 판정을 키고
        exit에서 다시 해제해주는 느낌
        */

        //actor.SetAni(AniState::Idle); 
        actor.SendMovePacket(0f, 0f);
        actor.SendStateChange(eState.Idle);
    }

    public void Execute()
    {
        if (!actor.IsLocal) return;

        // 공격 입력 체크 (좌/우클릭)
        PlayerActor pActor = actor as PlayerActor;
        if (pActor != null && pActor.CheckActionInput()) return;

        if (actor.h != 0 || actor.v != 0)
        {
            actor.sm.ChangeState(new MoveState(actor));
        }
    }

    public void Exit()
    {
        //현재 상태에서 초기화 해야 할 것들 idle에서는 따로 x
    }
}