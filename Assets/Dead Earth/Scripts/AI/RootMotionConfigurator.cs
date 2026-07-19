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

    //bug修复，新增变量，表示OnStateEnter（）已经执行，否则可能子状态还没有执行
    //，这里就先执行了，但这个时候还没有_stateMachine，导致没有增加计数,而之后却减了

    //“进入状态时，是否真的增加过计数？”
    private bool _rootMotionProcessed = false;  

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_stateMachine)
        {
            _stateMachine.AddRootMotionRequest(_rootPosition,_rootRotation);
            _rootMotionProcessed = true;
        }
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_stateMachine)
        {
            _stateMachine.AddRootMotionRequest(-_rootPosition,-_rootRotation);
            _rootMotionProcessed = false;
        }
    }
}
