using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine<T> where T : class
{
    private T entity;
    private State<T> currentState;
    private State<T> previousState;
    private State<T> globalState;

    public void Setup(T ownerOfStateMachine, State<T> defaultState)
    {
        entity = ownerOfStateMachine;
        currentState = null;
        previousState = null;
        globalState = null;

        ChangeState(defaultState);
    }

    public void Execute()
    {
        if (globalState != null) globalState.Execute(entity);
        if (currentState != null) currentState.Execute(entity);
    }

    public void ChangeState(State<T> newState)
    {
        if(newState == null) return;

        if (currentState != null)
        {
            previousState = currentState;
            currentState.Exit(entity);
        }

        currentState = newState;
        currentState.Enter(entity);
    }

    public void SetGlobalState(State<T> newState)
    {
        globalState = newState;
    }

    public void RevertToPriviousState()
    {
        ChangeState(previousState);
    }
}
