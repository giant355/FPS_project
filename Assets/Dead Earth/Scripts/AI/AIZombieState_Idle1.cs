using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIZombieState_Idle1 : AIZombieState
{
    [SerializeField] Vector2 _idleTimeRange = new Vector2(10.0f, 60.0f);

    float _idleTime = 0.0f;
    float _timer = 0.0f;

    public override AIStateType GetStateType()
    {
        //Debug.Log(" State Type being fetched by state machine");
        return AIStateType.Idle;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entering Idle State");
        base.OnEnterState();
        if (_zombieStateMachine == null)
            return;

        _idleTime = Random.Range(_idleTimeRange.x, _idleTimeRange.y);
        _timer = 0.0f;

        _zombieStateMachine.NavAgentControl(true, false );
        _zombieStateMachine.speed = 0;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;
        _zombieStateMachine.ClearTarget();
    }

    public override AIStateType OnUpdate()
    {
        if (_zombieStateMachine == null)
            return AIStateType.Idle;

        if (_zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Player)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        // Is the threat a flashlight
        if (_zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Light)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }

        // Is the threat an audio emitter
        if (_zombieStateMachine.AudioThreat.AITargetType == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }

        // Is the threat food
        if (_zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Food)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        _timer += Time.deltaTime;

        if (_timer > _idleTime)
        {
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));
            //_zombieStateMachine.navAgent.isStopped = false;
            return AIStateType.Alerted;
        }

        return AIStateType.Idle;
    }
}
