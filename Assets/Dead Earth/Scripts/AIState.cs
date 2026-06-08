using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIState
{
    AIStateType type;
    public void SetStateMachine(AIStateMachine stateMachine) { _stateMachine = stateMachine; }

    public virtual void OnEnterState() { }//像构造函数一样
    public virtual void OnExitState() { }//析构函数一样
    public virtual void OnAnimatorUpdated()
    {
        if (_stateMachine.useRootPosition)
            _stateMachine.navAgent.velocity = _stateMachine.animator.deltaPosition / Time.deltaTime;
        if (_stateMachine.useRootPosition)
            _stateMachine.transform.rotation = _stateMachine.animator.rootRotation;
    }
    public virtual void OnAnimatorIKUpdated() { }
    public virtual void OnTriggerEvent(AITriggerEventType eventType,Collider other) { }
    public virtual void OnDestinationReached(bool isReached) { }
  
    public abstract AIStateType GetStateType();
    public abstract AIStateType OnUpdate();

    protected AIStateMachine _stateMachine;
}
