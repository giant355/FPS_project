using UnityEngine;
using UnityEngine.AI;

// ----------------------------------------------------------------
// 类	:	AIZombieState_Patrol1
// 描述	:	僵尸的通用巡逻行为
// ----------------------------------------------------------------
//僵尸爱撞墙的两个主要原因：
//1.动画提前步
//2.wayPoint angle threshold太大，导致明明前面就是墙也要走
public class AIZombieState_Patrol1 : AIZombieState
{
    [SerializeField] float _turnOnSpotThreshold = 80.0f;
    [SerializeField] float _slerpSpeed = 5.0f;
    [SerializeField] [Range(0.0f, 3.0f)] float _speed = 1.0f;
    float _repathTimer;

    public override AIStateType GetStateType()
    {
        return AIStateType.Patrol;
    }

    public override void OnEnterState()
    {
        Debug.Log("Enter Patrol State");
        base.OnEnterState();

        _repathTimer = 0.2f;

        if (_zombieStateMachine == null)
            return;

        _zombieStateMachine.NavAgentControl(true, false);
        //_zombieStateMachine.speed = _speed;速度设置移步到70行左右
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        // 通过基类 GetWaypointPosition 获取路点并设置目标
        if(!_zombieStateMachine.navAgent.pathPending)
        _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));

        _zombieStateMachine.navAgent.isStopped = false;
    }

    public override AIStateType OnUpdate()
    {
        _repathTimer -= Time.deltaTime;
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
        //在这里设置速度，防止提前步
        if (_zombieStateMachine.navAgent.pathPending)
        {
            _zombieStateMachine.speed = 0;
            return AIStateType.Patrol;
        }
        else
            _zombieStateMachine.speed = _speed;

        float angle = Vector3.Angle(_zombieStateMachine.transform.forward,
            (_zombieStateMachine.navAgent.steeringTarget - _zombieStateMachine.transform.position));

        if (angle > _turnOnSpotThreshold)
            return AIStateType.Alerted;

        if (!_zombieStateMachine.useRootRotation)
        {
            Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navAgent.desiredVelocity);
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
        }

        if (((!_zombieStateMachine.navAgent.hasPath && !_zombieStateMachine.navAgent.pathPending) ||
            _zombieStateMachine.navAgent.pathStatus == NavMeshPathStatus.PathInvalid)&&_repathTimer<0)
        {
            _repathTimer = 0.2f;
            //print(_repathTimer);
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

    private float _lookWeight = 0f;

    public override void OnAnimatorIKUpdated()
    {
        if (_zombieStateMachine == null)
            return;

        // 0 = 不看, 1 = 全看
        float targetWeight = _zombieStateMachine.targetType != AITargetType.None ? 0.15f : 0f;

        _lookWeight = Mathf.Lerp(_lookWeight, targetWeight, Time.deltaTime*0.2f);

        _zombieStateMachine.animator.SetLookAtPosition(_zombieStateMachine.targetPosition + Vector3.up);
        _zombieStateMachine.animator.SetLookAtWeight(_lookWeight);
    }
}
