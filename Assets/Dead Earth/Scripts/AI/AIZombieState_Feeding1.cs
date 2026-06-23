using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIZombieState_Feeding1 : AIZombieState
{
    [SerializeField] float _slerpSpeed=3f;
    [SerializeField] Transform _bloodParticlesMount = null;
    [SerializeField][Range(0.01f, 1.0f)] float _bloodParticlesBurstTime = 0.1f;
    [SerializeField][Range(1, 100)] int _bloodParticlesBurstAmount = 10;
    /// <summary>
    /// zombie_eating的哈希
    /// </summary>
    private int _eatingStateHash = Animator.StringToHash("zombie_eating");
    /// <summary>
    /// Cinematic的层索引
    /// </summary>
    private int _eatingLayerIndex = -1;
    private float _timer = 0.0f;


    public override AIStateType GetStateType() { return AIStateType.Feeding; }
    // OnUpdate 每帧
    //├─ satisfaction > 0.9 → GetWaypointPosition → Alerted
    //├─ 视觉威胁（非食物）→ SetTarget → Alerted
    //├─ 音频威胁          → SetTarget → Alerted
    //├─ 播放进食动画中     → satisfaction 累加
    //├─ 旋转朝向食物（Slerp）
    //└─ 默认保持 Feeding
    public override void OnEnterState()
    {
        Debug.Log("Entering feeding State");
        base.OnEnterState();

        if (_zombieStateMachine == null) return;

        // 初始化layer index
        if (_eatingLayerIndex == -1)
            _eatingLayerIndex = _zombieStateMachine.animator.GetLayerIndex("Cinematic");

        //初始化状态机参数
        _zombieStateMachine.feeding = true;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.speed = 0;
        _zombieStateMachine.attackType = 0;


        _zombieStateMachine.NavAgentControl(true, false);
    }
    public override void OnExitState()
    {
        if (_zombieStateMachine != null)
        {
            _zombieStateMachine.feeding = false;
        }
    }
    public override AIStateType OnUpdate()
    {
        _timer += Time.deltaTime;

        print(_zombieStateMachine.feeding);
        if (_zombieStateMachine.satisfaction > 0.99f)
        {
            _zombieStateMachine.GetWaypointPosition(false);
            return AIStateType.Alerted;
        }

        if (_zombieStateMachine.VisualThreat.AITargetType != AITargetType.None && _zombieStateMachine.VisualThreat.AITargetType != AITargetType.Visual_Food)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }

        if (_zombieStateMachine.AudioThreat.AITargetType == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }

        if (_zombieStateMachine.animator.GetCurrentAnimatorStateInfo(_eatingLayerIndex).shortNameHash == _eatingStateHash)
        {
            _zombieStateMachine.satisfaction = Mathf.Min(_zombieStateMachine.satisfaction + ((Time.deltaTime * _zombieStateMachine.replenishRate) / 100.0f), 1.0f);
            if (GameSceneManager.Instance && GameSceneManager.Instance.bloodParticles && _bloodParticlesMount)
            {
                if (_timer > _bloodParticlesBurstTime)
                {
                    ParticleSystem system = GameSceneManager.Instance.bloodParticles;
                    system.transform.position = _bloodParticlesMount.transform.position;
                    system.transform.rotation = _bloodParticlesMount.transform.rotation;
                    var main = system.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    system.Emit(_bloodParticlesBurstAmount);
                    _timer = 0.0f;
                }

            }
        }
        print(_zombieStateMachine.useRootRotation);
        if (!_zombieStateMachine.useRootRotation)
        {
            //一直朝向目标
            Vector3 targetPos = _zombieStateMachine.targetPosition;
            targetPos.y = _zombieStateMachine.transform.position.y;
            Quaternion newRot = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
        }

        return AIStateType.Feeding;
    }
}
