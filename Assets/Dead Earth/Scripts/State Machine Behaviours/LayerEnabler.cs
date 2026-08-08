using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerEnabler : AIStateMachineLink
{
    /// <summary>
    /// 说明在当前层没有播放一个有效的动画
    /// </summary>
    public bool		OnEnter = false;
	/// <summary>
	/// 说明在当前层播放一个有效的动画
	/// </summary>
	public bool		OnExit  = false;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex )
	{
		if (_stateMachine)
			_stateMachine.SetLayerActive(animator.GetLayerName(layerIndex), OnEnter);
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex )
	{
		if (_stateMachine)
			_stateMachine.SetLayerActive(animator.GetLayerName(layerIndex), OnExit);
	}

}
