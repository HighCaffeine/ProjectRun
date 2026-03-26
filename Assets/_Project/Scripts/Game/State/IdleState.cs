using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class IdleState : IState
{
    public Actor actor;
    public Action onEndTrigger; //idle 종료 시 콜백

    public IdleState(Actor actor, Action onEndTrigger)
    {
        this.actor = actor;
        this.onEndTrigger = onEndTrigger;
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
    }

    public void Execute()
    {
        //idle 끝나는지 체크 여기서는 키 입력이 해제 조건
        //onEndTrigger실행헤주고
        if (actor.dir.x != 0.0f || actor.dir.y != 0.0f)
        {
            onEndTrigger?.Invoke();
        }
    }

    public void Exit()
    {
        //현재 상태에서 초기화 해야 할 것들 idle에서는 따로 x
    }
}