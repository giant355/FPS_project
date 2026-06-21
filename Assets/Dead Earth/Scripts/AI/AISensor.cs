using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 进入状态机监视器范围后，调用相应函数，状态机再在对应子状态调用相应函数
/// </summary>
public class AISensor:MonoBehaviour
{
    private AIStateMachine _parentStateMachine;
    public AIStateMachine parentStateMachine { set { _parentStateMachine = value; } }

    private void OnTriggerEnter(Collider other)
    {
        if (_parentStateMachine != null)
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Enter, other);
    }
    private void OnTriggerStay(Collider other)
    {
        print(other.gameObject.name);
        if (_parentStateMachine != null)
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Stay, other);
    }
    private void OnTriggerExit(Collider other)
    {
        if (_parentStateMachine != null)
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Exit, other);
    }
}
