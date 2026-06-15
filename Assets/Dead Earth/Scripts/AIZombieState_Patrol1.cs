using UnityEngine;
using UnityEngine.AI;

// ----------------------------------------------------------------
// 类	:	AIZombieState_Patrol1
// 描述	:	僵尸的通用巡逻行为
// ----------------------------------------------------------------
public class AIZombieState_Patrol1 : AIZombieState
{
    [SerializeField] float _turnOnSpotThreshold = 80.0f;
    [SerializeField] float _slerpSpeed = 5.0f;
    [SerializeField] [Range(0.0f, 3.0f)] float _speed = 1.0f;

    public override AIStateType GetStateType()
    {
        return AIStateType.Patrol;
    }

    public override void OnEnterState()
    {
        Debug.Log("Enter Patrol State");
        base.OnEnterState();
        if (_zombieStateMachine == null)
            return;

        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = _speed;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        // 通过基类 GetWaypointPosition 获取路点并设置目标
        _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));
        _zombieStateMachine.navAgent.isStopped = false;
    }

    public override AIStateType OnUpdate()
    {
        if (_zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Player)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        if (_zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Light)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }

        if (_zombieStateMachine.AudioThreat.AITargetType == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }

        if (_zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Food)
        {
            if ((1.0f - _zombieStateMachine.satisfaction) > (_zombieStateMachine.VisualThreat.distance / _zombieStateMachine.sensorRadius))
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Pursuit;
            }
        }

        float angle = Vector3.Angle(_zombieStateMachine.transform.forward,
            (_zombieStateMachine.navAgent.steeringTarget - _zombieStateMachine.transform.position));

        if (angle > _turnOnSpotThreshold)
            return AIStateType.Alerted;

        if (!_zombieStateMachine.useRootRotation)
        {
            Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navAgent.desiredVelocity);
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
        }

        if (_zombieStateMachine.navAgent.isPathStale ||
            (!_zombieStateMachine.navAgent.hasPath && !_zombieStateMachine.navAgent.pathPending) ||
            _zombieStateMachine.navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(true));
        }

        return AIStateType.Patrol;
    }

    public override void OnDestinationReached(bool isReached)
    {
        if (_zombieStateMachine == null || !isReached)
            return;

        if (_zombieStateMachine.targetType == AITargetType.Waypoint)
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(true));
    }

    public override void OnAnimatorIKUpdated()
    {
        if (_zombieStateMachine == null)
            return;

        _zombieStateMachine.animator.SetLookAtPosition(_zombieStateMachine.targetPosition + Vector3.up);
        _zombieStateMachine.animator.SetLookAtWeight(0.25f);
    }
}
