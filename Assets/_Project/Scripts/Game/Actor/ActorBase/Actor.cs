using System;
using UnityEngine;

public enum AniState {Idle, Move, Dead, Count};

public class Actor : MonoBehaviour
{
    public StateMachine sm = new StateMachine();
    public P_PacketVector3 dir;

    protected virtual void Start()
    {
        sm.ChangeState(new IdleState(this, () => { Move(); }));
    }

    protected virtual void Move()
    {
        //실제 이동
        //플레이어는 dir에 axis넣고, 몬스터는 타겟위치 방향 넣고
    }

    public void SetAni(AniState aniState)
    {
        switch (aniState)
        {
            case AniState.Idle: 
            break;
            case AniState.Move:
            break;
            case AniState.Dead:
            break;
        }
    }
}