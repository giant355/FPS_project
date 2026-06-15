using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// ----------------------------------------------------------------
// 类	:	AIZombieState_Patrol1
// 描述	:	僵尸的通用巡逻行为
// ----------------------------------------------------------------
public class AIZombieState_Patrol1 : AIZombieState
{
    //  Inspector 赋值 
    [SerializeField] AIWayPointNetwork _waypointNetwork = null;
    [SerializeField] bool _randomPatrol = false;
    [SerializeField] int _currentWaypoint = 0;
    [SerializeField] float _turnOnSpotThreshold = 80.0f;
    [SerializeField] float _slerpSpeed = 5.0f;

    [SerializeField] [Range(0.0f, 3.0f)] float _speed = 1.0f;

    // ------------------------------------------------------------
    // 名称	:	GetStateType
    // 描述	:	由父状态机调用，用于获取此状态的类型。
    // ------------------------------------------------------------
    public override AIStateType GetStateType()
    {
        return AIStateType.Patrol;
    }

    // ------------------------------------------------------------------
    // 名称	:	OnEnterState
    // 描述	:	首次转换到此状态时由状态机调用。它初始化一个计时器并配置状态机。
    // ------------------------------------------------------------------
    public override void OnEnterState()
    {
        Debug.Log("Enter Patrol State");
        base.OnEnterState();
        if (_zombieStateMachine == null)
            return;

        // 配置状态机
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = _speed;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        // 如果当前目标不是路点，则需要从路点网络中选择一个路点并将其设为新目标，并规划一条到达该点的路径
        if (_zombieStateMachine.targetType != AITargetType.Waypoint)
        {
            // 清除之前的任何目标
            _zombieStateMachine.ClearTarget();

            // 是否有有效的路点网络
            if (_waypointNetwork != null && _waypointNetwork.Waypoints.Count > 0)
            {
                // 如果是随机巡逻，则将当前路点设置为随机路点索引
                if (_randomPatrol)
                {
                    _currentWaypoint = Random.Range(0, _waypointNetwork.Waypoints.Count);
                }

                // 如果是有效索引，则获取路点并将其设为新目标
                if (_currentWaypoint < _waypointNetwork.Waypoints.Count)
                {
                    Transform waypoint = _waypointNetwork.Waypoints[_currentWaypoint];
                    if (waypoint != null)
                    {
                        // 这是新的状态机目标
                        _zombieStateMachine.SetTarget(AITargetType.Waypoint,
                                                        null,
                                                        waypoint.position,
                                                        Vector3.Distance(_zombieStateMachine.transform.position, waypoint.position)
                                                    );

                        // 告诉 NavAgent 生成一条到达该路点的路径
                        _zombieStateMachine.navAgent.SetDestination(waypoint.position);
                    }
                }
            }
        }

        // 确保 NavAgent 已开启
        _zombieStateMachine.navAgent.isStopped=false;
    }


    // ------------------------------------------------------------
    // 名称	:	OnUpdate
    // 描述	:	每帧由状态机调用，为此状态提供一个时间片来更新自身。
    //          它处理威胁和状态转换，并在不使用根旋转的情况下保持僵尸朝向正确方向。
    // ------------------------------------------------------------
    public override AIStateType OnUpdate()
    {
        // 是否有视觉威胁且是玩家
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

        // 声音是第三优先级
        if (_zombieStateMachine.AudioThreat.AITargetType == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }

        // 我们看到了尸体，如果饥饿度足够就追踪它
        if (_zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Food)
        {
            // 如果距离与饥饿度的比值意味着我们足够饥饿以至于愿意偏离路径那么远
            if ((1.0f - _zombieStateMachine.satisfaction) > (_zombieStateMachine.VisualThreat.distance / _zombieStateMachine.sensorRadius))
            {
                _stateMachine.SetTarget(_stateMachine.VisualThreat);
                return AIStateType.Pursuit;
            }
        }

        // 计算我们需要转动多少角度才能面向目标
        float angle = Vector3.Angle(_zombieStateMachine.transform.forward, (_zombieStateMachine.navAgent.steeringTarget - _zombieStateMachine.transform.position));

        // 如果角度太大，则退出巡逻状态进入警觉状态
        if (angle > _turnOnSpotThreshold)
        {
            return AIStateType.Alerted;
        }

        // 如果不使用根旋转，则我们负责保持僵尸的旋转和朝向正确
        if (!_zombieStateMachine.useRootRotation)
        {
            // 生成一个表示我们应该朝向的新四元数
            Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navAgent.desiredVelocity);

            // 随时间平滑旋转到新朝向
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
        }

        // 如果导航代理因任何原因丢失了路径，则调用 NextWaypoint 函数
        // 以便选择一个新的路点并为导航代理分配新路径。
        if (
            (!_zombieStateMachine.navAgent.hasPath && !_zombieStateMachine.navAgent.pathPending) ||
            (_zombieStateMachine.navAgent.pathStatus == NavMeshPathStatus.PathInvalid))
        {
            print("222");
            print($"hasPath:{_zombieStateMachine.navAgent.hasPath}");
            print($"isPathStale:{_zombieStateMachine.navAgent.isPathStale}");
            print($"pathStatus:{_zombieStateMachine.navAgent.pathStatus}");
            print($"pathPending:{_zombieStateMachine.navAgent.pathPending}");
            NextWaypoint();
        }


        // 保持在巡逻状态
        return AIStateType.Patrol;
    }

    // -------------------------------------------------------------------------
    // 名称	:	NextWaypoint
    // 描述	:	用于选择一个新路点。要么从路点网络中随机选择一个新的路点，
    //          要么递增当前路点索引（带环绕），按顺序访问网络中的路点。
    //          将新路点设为目标，并为其生成导航代理路径。
    // -------------------------------------------------------------------------
    private void NextWaypoint()
    {
        // 增加当前路点索引，带环绕归零（或选择一个随机路点）
        if (_randomPatrol && _waypointNetwork.Waypoints.Count > 1)
        {
            // 持续生成随机路点，直到找到一个与当前路点不同的点
            // 注意：路点网络不能只有一个路点，这一点非常重要 :)
            int oldWaypoint = _currentWaypoint;
            while (_currentWaypoint == oldWaypoint)
            {
                _currentWaypoint = Random.Range(0, _waypointNetwork.Waypoints.Count);
            }
        }
        else
            _currentWaypoint = _currentWaypoint == _waypointNetwork.Waypoints.Count - 1 ? 0 : _currentWaypoint + 1;

        // 从路点列表中获取新路点
        if (_waypointNetwork.Waypoints[_currentWaypoint] != null)
        {
            Transform newWaypoint = _waypointNetwork.Waypoints[_currentWaypoint];

            // 这是我们的新目标位置
            _zombieStateMachine.SetTarget(AITargetType.Waypoint,
                                            null,
                                            newWaypoint.position,
                                            Vector3.Distance(newWaypoint.position, _zombieStateMachine.transform.position));

            // 设置新路径
            _zombieStateMachine.navAgent.SetDestination(newWaypoint.position);
        }
        //print("111");
    }

    // ----------------------------------------------------------------------
    // 名称	:	OnDestinationReached
    // 描述	:	当僵尸到达其目标（进入目标触发器）时，由父状态机调用。
    // ----------------------------------------------------------------------
    public override void OnDestinationReached(bool isReached)
    {
        // 只处理到达事件，不处理离开事件
        if (_zombieStateMachine == null || !isReached)
            return;

        // 在路点网络中选择下一个路点
        if (_zombieStateMachine.targetType == AITargetType.Waypoint)
            NextWaypoint();
        //print("000");
    }

    // -----------------------------------------------------------------------
    // 名称	:	OnAnimatorIKUpdated
    // 描述	:	重写 IK 目标
    // -----------------------------------------------------------------------
    public override void OnAnimatorIKUpdated()
    {
        if (_zombieStateMachine == null)
            return;

        _zombieStateMachine.animator.SetLookAtPosition(_zombieStateMachine.targetPosition + Vector3.up);
        _zombieStateMachine.animator.SetLookAtWeight(0.25f);
    }
}