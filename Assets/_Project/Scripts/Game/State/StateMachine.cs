using System;
using UnityEngine;

public enum eState
{
    Idle = 0,
    Move = 1,
    Push = 2,
    Pull = 3,
    Dash = 4,
    Knockback = 5,
    Teleport = 6,
    State_Count = 7,
    Escape = 8,
};

public interface IState
{
    public void Enter();        //state 시작 (함수 등록)
    public void Execute();      //state 동작 (함수 호출)
    public void Exit();         //state 해제 (함수 해제)
}

public class StateMachine
{
    public IState currentState { get; private set; }

    public void ChangeState(IState state)
    {
        currentState?.Exit();
        currentState = state;
        currentState.Enter();
    }

    public void Update() => currentState.Execute();
}