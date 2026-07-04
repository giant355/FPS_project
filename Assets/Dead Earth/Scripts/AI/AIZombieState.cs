using UnityEngine;
using System.Collections;

// -----------------------------------------------------------------------------------------
// CLASS	:	AIZombieState
// DESC		:	所有僵尸状态的直接基类。提供事件处理以及当前威胁的存储。
// ----------------------------------------------------------------------------------------
public abstract class AIZombieState : AIState
{
    // Private
    protected int _playerLayerMask = -1;
    protected int _bodyPartLayer = -1;
    protected int _visualLayerMask = -1;
    protected AIZombieStateMachine _zombieStateMachine = null;

    // -------------------------------------------------------------------------------------
    // Name	:	Awake
    // Desc	:	计算用于射线检测和层级测试的遮罩和层级索引
    // -------------------------------------------------------------------------------------
    void Awake()
    {
        // 获取用于与玩家进行视线测试的遮罩
        _playerLayerMask = LayerMask.GetMask("Player", "AI Body Part", "Default");
        _visualLayerMask = LayerMask.GetMask("Player", "AI Body Part", "Visual Aggravator", "Default");

        // 获取AI Body Part层的索引
        _bodyPartLayer = LayerMask.NameToLayer("AI Body Part");
    }

    // -------------------------------------------------------------------------------------
    // Name	:	SetStateMachine
    // Desc	:	检查类型兼容性并将引用存储为派生类型
    // -------------------------------------------------------------------------------------
    public override void SetStateMachine(AIStateMachine stateMachine)
    {
        if (stateMachine.GetType() == typeof(AIZombieStateMachine))
        {
            base.SetStateMachine(stateMachine);
            _zombieStateMachine = (AIZombieStateMachine)stateMachine;
        }
    }

    // -------------------------------------------------------------------------------------
    // Name	:	OnTriggerEvent
    // Desc	:	由父状态机调用，当威胁进入/停留/离开僵尸的传感器触发器时触发。
    //			包括任何属于Visual或Audio Aggravator层的碰撞体，或玩家。
    //			该方法会检查威胁，如果发现比当前存储的威胁优先级更高，则存入父状态机
    //			的VisualThreat或AudioThreat成员中。
    // --------------------------------------------------------------------------------------
    public override void OnTriggerEvent(AITriggerEventType eventType, Collider other)
    {
        // 如果没有父状态机则退出
        if (_zombieStateMachine == null)
            return;

        //print("other:"+other.name);
        //print("other.Tag:"+other.tag);

        // 不关心退出事件，只处理进入和停留
        if (eventType != AITriggerEventType.Exit)
        {
            // 当前已存储的视觉威胁类型
            AITargetType curType = _zombieStateMachine.VisualThreat.AITargetType;

            // 进入传感器的碰撞体是玩家吗
            if (other.CompareTag("Player"))
            {
                // 计算传感器原点到碰撞体的距离
                float distance = Vector3.Distance(_zombieStateMachine.sensorPosition, other.transform.position);

                // 如果当前存储的威胁不是玩家，或者这个玩家比之前存储的更近，则更重要
                if (curType != AITargetType.Visual_Player ||
                    (curType == AITargetType.Visual_Player && distance < _zombieStateMachine.VisualThreat.distance))
                {
                    // 检查该碰撞体是否在视野范围内且有视线畅通s
                    RaycastHit hitInfo;
                    if (ColliderIsVisible(other, out hitInfo, _playerLayerMask))
                    {
                print("i can see player");
                        // 在视野内且距离近，视线畅通，存储为当前最危险的威胁
                        _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Player, other, other.transform.position, distance);
                    }
                }
            }
            else
            if (other.CompareTag("Flash Light") && curType != AITargetType.Visual_Player)
            {
                BoxCollider flashLightTrigger = (BoxCollider)other;
                float distanceToThreat = Vector3.Distance(_zombieStateMachine.sensorPosition, flashLightTrigger.transform.position);
                float zSize = flashLightTrigger.size.z * flashLightTrigger.transform.lossyScale.z;
                float aggrFactor = distanceToThreat / zSize;
                if (aggrFactor <= _zombieStateMachine.sight && aggrFactor <= _zombieStateMachine.intelligence)
                {
                    _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Light, other, other.transform.position, distanceToThreat);
                }
            }
            else
            if (other.CompareTag("AI Sound Emitter"))
            {
                SphereCollider soundTrigger = (SphereCollider)other;
                if (soundTrigger == null) return;

                // 获取传感器位置
                Vector3 agentSensorPosition = _zombieStateMachine.sensorPosition;

                Vector3 soundPos;
                float soundRadius;
                AIState.ConvertSphereColliderToWorldSpace(soundTrigger, out soundPos, out soundRadius);

                // 我们在声音半径内多深的位置
                float distanceToThreat = (soundPos - agentSensorPosition).magnitude;

                // 计算距离因子：在声音边缘时=1.0，在中心时=0
                float distanceFactor = (distanceToThreat / soundRadius);

                // 根据AI的听力能力调整因子
                distanceFactor += distanceFactor * (1.0f - _zombieStateMachine.hearing);

                // 太远了，听不见
                if (distanceFactor > 1.0f)
                    return;

                // 我们能听到它，且比之前存储的更近吗
                if (distanceToThreat < _zombieStateMachine.AudioThreat.distance)
                {
                    // 目前为止最危险的音频威胁
                    _zombieStateMachine.AudioThreat.Set(AITargetType.Audio, other, soundPos, distanceToThreat);
                }
            }
            else
            // 记录最近的视觉食物威胁
            if (other.CompareTag("AI Food") && curType != AITargetType.Visual_Player && curType != AITargetType.Visual_Light &&
                _zombieStateMachine.satisfaction <= 0.9f && _zombieStateMachine.AudioThreat.AITargetType == AITargetType.None)
            {
                // 威胁离我们有多远
                float distanceToThreat = Vector3.Distance(other.transform.position, _zombieStateMachine.sensorPosition);

                // 比之前存储的更近吗
                if (distanceToThreat < _zombieStateMachine.VisualThreat.distance)
                {
                    // 检查是否在视野范围内且有视线畅通
                    RaycastHit hitInfo;
                    if (ColliderIsVisible(other, out hitInfo, _visualLayerMask))
                    {
                        // 目前为止最有吸引力的目标
                        _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Food, other, other.transform.position, distanceToThreat);
                    }
                }
            }

        }
    }

    // -------------------------------------------------------------------------------------
    // Name	:	ColliderIsVisible
    // Desc	:	根据僵尸的视野角度和传入的层级遮罩，测试碰撞体是否可见（视野+视线）
    // -------------------------------------------------------------------------------------
    protected virtual bool ColliderIsVisible(Collider other, out RaycastHit hitInfo, int layerMask = -1)
    {
        //print("check visable");
        // 确保有值可返回
        hitInfo = new RaycastHit();

        // 状态机必须为AIZombieStateMachine类型
        if (_zombieStateMachine == null) return false;

        // 计算传感器原点到碰撞体方向的夹角
        Vector3 head = _zombieStateMachine.sensorPosition;
        Vector3 direction = other.transform.position - head;
        float angle = Vector3.Angle(direction, _zombieStateMachine.transform.forward);

        // 如果夹角大于视野的一半，说明在视野锥之外，不可见
        if (angle > _zombieStateMachine.fov * 0.5f)
            return false;

        // 进行视线检测：从传感器原点向碰撞体方向发射射线，
        // 距离 = 传感器半径 × 视力，返回所有击中结果
        RaycastHit[] hits = Physics.RaycastAll(head, direction.normalized, _zombieStateMachine.sensorRadius * _zombieStateMachine.sight, layerMask);

        // 找出最近的非自身身体部件碰撞体。如果最近的不是目标，则目标被遮挡
        float closestColliderDistance = float.MaxValue;
        Collider closestCollider = null;

        // 遍历所有击中结果
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            // 这个击中点比之前找到的都近吗
            if (hit.distance < closestColliderDistance)
            {
                // 如果击中的是身体部件层
                if (hit.transform.gameObject.layer == _bodyPartLayer)
                {
                    // 确保不是我们自己的身体部件
                    // BUG修复：原代码用hit.rigidbody.GetInstanceID()查询，但注册的key是Collider的ID，
                    // 两者为不同Component，ID不匹配，导致自身Body Part无法被正确过滤。
                    // 改为用hit.transform.root.GetInstanceID()，与Awake中注册的根Transform ID匹配。
                    if (_zombieStateMachine != GameSceneManager.Instance.GetAIStateMachine(hit.transform.root.GetInstanceID()))
                    {
                        // 存储碰撞体、距离和击中信息
                        closestColliderDistance = hit.distance;
                        closestCollider = hit.collider;
                        hitInfo = hit;
                    }
                }
                else
                {
                    // 不是身体部件，直接存储为当前最近的命中
                    closestColliderDistance = hit.distance;
                    closestCollider = hit.collider;
                    hitInfo = hit;
                }
            }
        }

        // 如果最近的碰撞体就是我们要测试的目标，说明视线畅通
        if (closestCollider && closestCollider.gameObject == other.gameObject) 
            return true;

        // 否则有东西挡在目标和僵尸之间，视线被遮挡
        return false;
    }
}
