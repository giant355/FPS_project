using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIZombieState_Alerted1 : AIZombieState
{
    [SerializeField][Range(1, 60)] float _maxDuration = 10.0f;
    /// <summary>
    /// 面对wayPoint的对准范围角
    /// </summary>
    [SerializeField] float _waypointAngleThreshold = 90.0f;
    /// <summary>
    /// 面对威胁的对准范围角
    /// </summary>
    [SerializeField] float _threatAngleThreshold = 10.0f;

    float _timer=0;
    public override AIStateType GetStateType()
    {
		return AIStateType.Alerted;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entering Alerted State");
        base.OnEnterState();
        if (_zombieStateMachine == null)
            return;

        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = 0;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        _timer = _maxDuration;
    }

    public override AIStateType OnUpdate()
    {
        _timer -= Time.deltaTime;
        //如果_timer<0,重新patrol
        if (_timer <= 0.0f) return AIStateType.Patrol;
        //看到玩家，直接pursuit
        if (_zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Player)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }
        //没看到玩家，但听到声音，继续Alerted，重置时间
        if (_zombieStateMachine.AudioThreat.AITargetType == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            _timer = _maxDuration;
        }
        //没看到玩家，没听到声音，但看见光，继续Alerted，重置时间
        if (_zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Light)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            _timer = _maxDuration;
        }
        //食物最低优先级
        if (_zombieStateMachine.AudioThreat.AITargetType == AITargetType.None &&
            _zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Food)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        float angle;

        if(_zombieStateMachine.targetType==AITargetType.Audio||_zombieStateMachine.targetType==AITargetType.Visual_Light)
        {
            angle = AIState.FindSignedAngle(_zombieStateMachine.transform.forward,
                                            _zombieStateMachine.targetPosition - _zombieStateMachine.transform.position);
            //与声源的夹角小于threatAngleThreshold，直接追击
            if (_zombieStateMachine.targetType == AITargetType.Audio && Mathf.Abs(angle) < _threatAngleThreshold)
            {
                return AIStateType.Pursuit;
            }
            //智力水平较高，更加可能按照正确方向转身
            if (Random.value < _zombieStateMachine.intelligence)
            {
                _zombieStateMachine.seeking = (int)Mathf.Sign(angle);
            }
            //智力水平低，更可能随机转身
            else
            {
                _zombieStateMachine.seeking = (int)Mathf.Sign(Random.Range(-1.0f, 1.0f));
            }
        }
        else
        if (_zombieStateMachine.targetType == AITargetType.Waypoint)
        {
            angle = AIState.FindSignedAngle(_zombieStateMachine.transform.forward,
                                            _zombieStateMachine.navAgent.steeringTarget - _zombieStateMachine.transform.position);
            //与wayPoint夹角小于waypointAngleThreshold，直接巡逻
            if (Mathf.Abs(angle) < _waypointAngleThreshold) return AIStateType.Patrol;
            _zombieStateMachine.seeking = (int)Mathf.Sign(angle);
        }

        return AIStateType.Alerted;
    }
}
   
