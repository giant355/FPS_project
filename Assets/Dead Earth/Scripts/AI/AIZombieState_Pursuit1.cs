using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// -----------------------------------------------------------------
// 类	: AIZombieState_Pursuit1
// 描述	: 僵尸用于追击目标的状态
// -----------------------------------------------------------------
public class AIZombieState_Pursuit1 : AIZombieState
{
    [SerializeField][Range(0, 10)] private float _speed = 1.0f;
    [SerializeField] private float _slerpSpeed = 5.0f;

    // --- 可见玩家的重寻路参数 ---
    [Header("Visual Player Repathing")]
    // 以此距离为界：近距离需更快响应，远距离可降低路径更新频率。
    [SerializeField] private float _visualRepathNearDistance = 3.0f;
    // 僵尸离玩家较近但尚未进入攻击范围时，允许重寻路的最短时间间隔。
    [SerializeField] private float _visualRepathNearInterval = 0.2f;
    // 僵尸离玩家较远时，允许重寻路的最短时间间隔。
    [SerializeField] private float _visualRepathFarInterval = 0.5f;
    // 近距离时，玩家累计移动至少这么远才值得重新计算路径。
    [SerializeField] private float _visualRepathNearMoveThreshold = 0.25f;
    // 远距离时，小幅移动对路线影响很小；达到此距离才重新计算路径。
    [SerializeField] private float _visualRepathFarMoveThreshold = 0.5f;
    [SerializeField] private float _repathDistanceMultiplier = 0.035f;    // 音频威胁重新寻路的距离乘数
    [SerializeField] private float _repathVisualMinDuration = 0.05f;      // 光源视觉威胁重新寻路的最小间隔（秒）
    [SerializeField] private float _repathVisualMaxDuration = 5.0f;       // 光源视觉威胁重新寻路的最大间隔（秒）
    [SerializeField] private float _repathAudioMinDuration = 0.25f;       // 音频威胁重新寻路的最小时间间隔（秒）
    [SerializeField] private float _repathAudioMaxDuration = 5.0f;        // 音频威胁重新寻路的最大时间间隔（秒）
    [SerializeField] private float _maxDuration = 40.0f;

    // 私有字段
    private float _timer = 0.0f;
    // 距离上一次 SetDestination 已经过的时间；音频、光源与玩家重寻路共用它。
    private float _repathTimer = 0.0f;
    // 上一次真正发送给 NavMeshAgent 的玩家水平位置。
    // 不直接比较 targetPosition，因为 targetPosition 会随 SetTarget 每帧更新。
    private Vector3 _lastVisualRepathPosition;
    // 是否已经记录过上一次视觉玩家寻路位置；首次追到玩家时需要立即建立记录。
    private bool _hasVisualRepathPosition;

    // 必须重写的方法
    public override AIStateType GetStateType() { return AIStateType.Pursuit; }

    // 默认处理器
    public override void OnEnterState()
    {
        Debug.Log("Entering Pursuit State");

        base.OnEnterState();
        if (_zombieStateMachine == null)
            return;

        // 配置状态机
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = _speed;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        // 僵尸只会追击有限时间，之后会放弃
        _timer = 0.0f;
        _repathTimer = 0.0f;
        _lastVisualRepathPosition = _zombieStateMachine.targetPosition;
        _hasVisualRepathPosition = true;


        // 设置目标路径(_target.position)
        //if( _zombieStateMachine.navAgent != null )
        _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.targetPosition);
        _zombieStateMachine.navAgent.isStopped = false;

    }

    // ---------------------------------------------------------------------
    // 名称	: OnUpdateAI
    // 描述	: 此状态的核心逻辑
    // ---------------------------------------------------------------------

    // 开始
    //  ├─ 超时？→ 切换到 Patrol（巡逻）
    //  ├─ 追击玩家 + 进入近战范围？→ 切换到 Attack（攻击）
    //  ├─ 已到达目标？
    //  │   ├─ 音频/光源 → ClearTarget → Alerted（警觉）
    //  │   └─ 食物 → Feeding（进食）
    //  ├─ 导航路径丢失？→ Alerted
    //  ├─ 旋转更新逻辑
    //  └─ 威胁优先级判断
    //  ├─ 视觉玩家威胁 → 持续追击（动态重新寻路）
    //  ├─ 当前目标是玩家最后位置 → 持续追击
    //  ├─ 视觉光源威胁 → 视情况 Alerted 或继续追击
    //  └─ 音频威胁 → 视情况 Alerted 或继续追击
    public override AIStateType OnUpdate()
    {
        _timer += Time.deltaTime;
        _repathTimer += Time.deltaTime;

        if (_timer > _maxDuration)
            return AIStateType.Patrol;

        // 如果正在追击玩家并且进入了近战触发范围，则攻击
        if (_zombieStateMachine.targetType == AITargetType.Visual_Player && _zombieStateMachine.inMeleeRange)
        {
            return AIStateType.Attack;
        }

        if (_zombieStateMachine.isTargetReached)
        {
            switch (_zombieStateMachine.targetType)
            {

                // 如果已经到达了源头
                case AITargetType.Audio:
                case AITargetType.Visual_Light:
                    _zombieStateMachine.ClearTarget();    // 清除威胁
                    return AIStateType.Alerted;     // 进入警觉状态并扫描目标

                case AITargetType.Visual_Food:
                    //进入前先预设进食半径，之后调回来
                    return AIStateType.Feeding;
            }
        }


        // 如果由于某种原因导航代理丢失了路径，则转入警觉状态
        // 这样它会尝试重新获取目标，或者最终放弃并恢复巡逻
        if (_zombieStateMachine.navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            return AIStateType.Alerted;
        }

        // 玩家仍在视野内时，可在下面重新设置目标；不要因旧路径已经走完而提前退出追击。
        if (!_zombieStateMachine.navAgent.pathPending && !_zombieStateMachine.navAgent.hasPath &&
            _zombieStateMachine.VisualThreat.AITargetType != AITargetType.Visual_Player)
        {
            return AIStateType.Alerted;
        }


        // 如果接近玩家目标，并且玩家仍在视野中，且是玩家类型，则持续面向玩家
        if (!_zombieStateMachine.useRootRotation && _zombieStateMachine.targetType == AITargetType.Visual_Player &&
                                     _zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Player && _zombieStateMachine.isTargetReached)
        {
            Vector3 targetPos = _zombieStateMachine.targetPosition;
            targetPos.y = _zombieStateMachine.transform.position.y;
            Quaternion newRot = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);
            _zombieStateMachine.transform.rotation = newRot;
        }
        else
        // 缓慢更新旋转以匹配导航代理的期望方向，但仅当不是追击玩家且未非常接近玩家时
        if (!_zombieStateMachine.useRootRotation && !_zombieStateMachine.isTargetReached &&
             _zombieStateMachine.navAgent.desiredVelocity != Vector3.zero)
        {
            Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navAgent.desiredVelocity);
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot,
        Time.deltaTime * _slerpSpeed);
        }
        else
        if (_zombieStateMachine.isTargetReached)
        {
            return AIStateType.Alerted;
        }

        // 是否有视觉威胁是玩家
        if (_zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Player)
        {
            UpdateVisualPlayerPath();

            // 确保这是当前目标
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);

            // 保持在追击状态
            return AIStateType.Pursuit;
        }

        // 如果当前目标是玩家的最后目击位置，则保持追击，因为没有其他东西可以覆盖
        if (_zombieStateMachine.targetType == AITargetType.Visual_Player)
            return AIStateType.Pursuit;




        // 如果有视觉威胁是玩家的光源
        if (_zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Light)
        {
            // 并且当前有一个较低优先级的目标，则转入警觉模式并尝试找到光源来源
            if (_zombieStateMachine.targetType == AITargetType.Audio || _zombieStateMachine.targetType == AITargetType.Visual_Food)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Alerted;
            }
            else
            if (_zombieStateMachine.targetType == AITargetType.Visual_Light)
            {
                // 获取目标碰撞体的唯一 ID
                int currentID = _zombieStateMachine.targetColliderID;

                // 如果是同一个光源
                if (currentID == _zombieStateMachine.VisualThreat.collider.GetInstanceID())
                {
                    // 位置不同 - 可能是同一威胁但已移动，因此定期重新寻路
                    if (_zombieStateMachine.targetPosition != _zombieStateMachine.VisualThreat.position)
                    {
                        // 离目标越近，重新寻路越频繁（尝试节省 CPU 开销）
                        if (Mathf.Clamp(_zombieStateMachine.VisualThreat.distance * _repathDistanceMultiplier, _repathVisualMinDuration, _repathVisualMaxDuration) < _repathTimer)
                        {
                            // 为代理重新寻路
                            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.VisualThreat.position);
                            _repathTimer = 0.0f;
                        }
                    }

                    _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                    return AIStateType.Pursuit;
                }
                else
                {
                    _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                    return AIStateType.Alerted;
                }
            }
        }

        else
        if (_zombieStateMachine.AudioThreat.AITargetType == AITargetType.Audio)
        {

            if (_zombieStateMachine.targetType == AITargetType.Visual_Food)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                return AIStateType.Alerted;
            }
            else
            if (_zombieStateMachine.targetType == AITargetType.Audio)
            {
                // 获取目标碰撞体的唯一 ID
                int currentID = _zombieStateMachine.targetColliderID;

                // 如果是同一个音频源
                if (currentID == _zombieStateMachine.AudioThreat.collider.GetInstanceID())
                {
                    // 位置不同 - 可能是同一威胁但已移动，因此定期重新寻路
                    if (_zombieStateMachine.targetPosition != _zombieStateMachine.AudioThreat.position)
                    {
                        // 离目标越近，重新寻路越频繁（尝试节省 CPU 开销）
                        if (Mathf.Clamp(_zombieStateMachine.AudioThreat.distance * _repathDistanceMultiplier, _repathAudioMinDuration, _repathAudioMaxDuration) < _repathTimer)
                        {
                            // 为代理重新寻路
                            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.AudioThreat.position);
                            _repathTimer = 0.0f;
                        }
                    }

                    _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                    return AIStateType.Pursuit;
                }
                else
                {
                    _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                    return AIStateType.Alerted;
                }
            }
        }

        // 默认
        return AIStateType.Pursuit;
    }

    /// <summary>
    /// 对可见玩家进行节流重新寻路。
    /// 近距离时用更短间隔，远距离时最多每半秒一次；玩家没有移动足够远时不重算路径。
    /// 进入近战范围会在 OnUpdate 开头切换到 Attack，因此攻击过程中不会调用这里。
    /// </summary>
    private void UpdateVisualPlayerPath()
    {
        Vector3 playerPosition = _zombieStateMachine.VisualThreat.position;
        Vector3 zombiePosition = _zombieStateMachine.transform.position;

        // NavMesh 只在水平面上寻路；忽略高度差，避免蹲下或高度变化造成无意义的重算。
        playerPosition.y = 0.0f;
        zombiePosition.y = 0.0f;

        float distance = Vector3.Distance(zombiePosition, playerPosition);
        bool isNearPlayer = distance <= _visualRepathNearDistance;
        float interval = isNearPlayer ? _visualRepathNearInterval : _visualRepathFarInterval;
        float moveThreshold = isNearPlayer ? _visualRepathNearMoveThreshold : _visualRepathFarMoveThreshold;

        Vector3 positionDelta = playerPosition - _lastVisualRepathPosition;
        positionDelta.y = 0.0f;
        bool playerMovedEnough = !_hasVisualRepathPosition ||
                                 positionDelta.sqrMagnitude >= moveThreshold * moveThreshold;
        bool switchedToPlayer = _zombieStateMachine.targetType != AITargetType.Visual_Player;
        bool pathHasFinished = !_zombieStateMachine.navAgent.pathPending && !_zombieStateMachine.navAgent.hasPath;

        if (switchedToPlayer || ((playerMovedEnough || pathHasFinished) && _repathTimer >= interval))
        {
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.VisualThreat.position);
            _lastVisualRepathPosition = playerPosition;
            _hasVisualRepathPosition = true;
            _repathTimer = 0.0f;
        }
    }

}
