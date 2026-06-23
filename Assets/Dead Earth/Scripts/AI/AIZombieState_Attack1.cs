using UnityEngine;
using System.Collections;

public class AIZombieState_Attack1 : AIZombieState
{
    // Inspector 面板设置
    [SerializeField][Range(0, 10)] float _speed = 0.0f;
    [SerializeField][Range(0.0f, 1.0f)] float _lookAtWeight = 0.7f;
    [SerializeField][Range(0.0f, 90.0f)] float _lookAtAngleThreshold = 15.0f;
    [SerializeField] float _slerpSpeed = 5.0f;
    [SerializeField] float _verticalOffset = 0.5f;


    // 私有变量
    private float _currentLookAtWeight = 0.0f;

    // 必须重写的方法
    public override AIStateType GetStateType() { return AIStateType.Attack; }

    // 默认处理函数
    public override void OnEnterState()
    {
        Debug.Log("Entering Attack State");

        base.OnEnterState();
        if (_zombieStateMachine == null)
            return;

        // 配置状态机
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = Random.Range(1, 100); ;
        _zombieStateMachine.speed = _speed;
        _currentLookAtWeight = 0.0f;
    }

    public override void OnExitState()
    {
        _zombieStateMachine.attackType = 0;
    }

    public override AIStateType OnUpdate()
    {
        Vector3 targetPos;
        Quaternion newRot;

        // 是否看到了玩家这个视觉威胁
        if (_zombieStateMachine.VisualThreat.AITargetType == AITargetType.Visual_Player)
        {
            // 设置新的目标
            _zombieStateMachine.SetTarget(_stateMachine.VisualThreat);

            // 如果已经不在近战范围内，就切换回追击状态
            if (!_zombieStateMachine.inMeleeRange) return AIStateType.Pursuit;

            if (!_zombieStateMachine.useRootRotation)
            {
                // 让僵尸始终面向玩家
                targetPos = _zombieStateMachine.targetPosition;
                targetPos.y = _zombieStateMachine.transform.position.y;
                newRot = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);
                _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
            }

            _zombieStateMachine.attackType = Random.Range(1, 100);

            return AIStateType.Attack;
        }

        // 玩家已经离开视野范围或躲起来了，所以先朝玩家最后的方向转过去
        // 然后退回警觉状态，让 AI 有机会重新发现目标
        if (!_zombieStateMachine.useRootRotation)
        {
            targetPos = _zombieStateMachine.targetPosition;
            targetPos.y = _zombieStateMachine.transform.position.y;
            newRot = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);
            _zombieStateMachine.transform.rotation = newRot;
        }

        // 进入警觉状态
        return AIStateType.Alerted;
    }

    public override void OnAnimatorIKUpdated()
    {
        if (_zombieStateMachine == null)
            return;

        if (Vector3.Angle(_zombieStateMachine.transform.forward, _zombieStateMachine.targetPosition - _zombieStateMachine.transform.position) < _lookAtAngleThreshold)
        {
            _zombieStateMachine.animator.SetLookAtPosition(_zombieStateMachine.targetPosition + Vector3.up * _verticalOffset);
            _currentLookAtWeight = Mathf.Lerp(_currentLookAtWeight, _lookAtWeight, Time.deltaTime);
            _zombieStateMachine.animator.SetLookAtWeight(_currentLookAtWeight);
        }
        else
        {
            _currentLookAtWeight = Mathf.Lerp(_currentLookAtWeight, 0.0f, Time.deltaTime);
            _zombieStateMachine.animator.SetLookAtWeight(_currentLookAtWeight);
        }
    }
}
