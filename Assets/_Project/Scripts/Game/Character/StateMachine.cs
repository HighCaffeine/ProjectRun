using System;
using UnityEngine;

public interface IState
{
    public void Enter();        //state 시작 (함수 등록)
    public void Execute();      //state 동작 (함수 호출)
    public void Exit();         //state 해제 (함수 해제)
}

public class StateMachine : MonoBehaviour
{
    public IState currentState { get; private set; }

    public void ChangeState(IState state)
    {
        currentState?.Exit();
        currentState = state;
        currentState.Enter();
    }
}