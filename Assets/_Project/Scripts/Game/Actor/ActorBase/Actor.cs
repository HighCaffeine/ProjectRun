using System;
using UnityEngine;

public enum AniState { Idle, Move, Dead, Count };

public class Actor : MonoBehaviour
{
    public StateMachine sm = new StateMachine();
    public P_PacketVector3 dir;

    protected virtual void Start()
    {

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