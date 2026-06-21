using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 在animator面板里面修改rootPosition和rootRotation，
/// 导致AddRootMotionRequest改变状态机的useRootPosition和useRootRotation，
/// 进一步，子状态的OnAnimatorUpdated判断是否启用根运动和根旋转
/// </summary>
public class RootMotionConfigurator :AIStateMachineLink
{
    [SerializeField] private int _rootPosition = 0;
    [SerializeField] private int _rootRotation = 0;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_stateMachine)
            _stateMachine.AddRootMotionRequest(_rootPosition,_rootRotation);
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_stateMachine)
            _stateMachine.AddRootMotionRequest(-_rootPosition,-_rootRotation);
    }
}
