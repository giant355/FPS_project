using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIState:MonoBehaviour
{
    AIStateType type;
    public virtual void SetStateMachine(AIStateMachine stateMachine) { _stateMachine = stateMachine; }

    public virtual void OnEnterState() { }//像构造函数一样
    public virtual void OnExitState() { }//析构函数一样
    public virtual void OnAnimatorUpdated()
    {
        if (_stateMachine.useRootPosition)
            _stateMachine.navAgent.velocity = _stateMachine.animator.deltaPosition / Time.deltaTime;
            if (_stateMachine.useRootRotation)
            _stateMachine.transform.rotation = _stateMachine.animator.rootRotation;
    }
    public virtual void OnAnimatorIKUpdated() { }
    public virtual void OnTriggerEvent(AITriggerEventType eventType,Collider other) { }
    public virtual void OnDestinationReached(bool isReached) { }
  
    public abstract AIStateType GetStateType();
    public abstract AIStateType OnUpdate();

    protected AIStateMachine _stateMachine;

    public static void ConvertSphereColliderToWorldSpace(SphereCollider col, out Vector3 pos, out float radius)
    {
        pos = Vector3.zero;
        radius = 0.0f;

        if (col == null)
            return;

        pos = col.transform.position;
        pos.x += col.center.x * col.transform.lossyScale.x;
        pos.y += col.center.y * col.transform.lossyScale.y;
        pos.y += col.center.z * col.transform.lossyScale.z;

        radius = Mathf.Max(col.radius * col.transform.lossyScale.x,
                            col.radius * col.transform.lossyScale.y);

        radius = Mathf.Max(radius, col.radius * col.transform.lossyScale.z);
    }
    /// <summary>
    /// XZ水平面有向角度，忽略高度差
    /// </summary>
    /// <param name="fromVector"></param>
    /// <param name="toVector"></param>
    /// <returns></returns>
    public static float FindSignedAngle(Vector3 fromVector, Vector3 toVector)
    {
        // Only yaw matters here. Ignore height so chest/head hits can still count as center hits.
        fromVector.y = 0.0f;
        toVector.y = 0.0f;

        if (fromVector.sqrMagnitude < 0.0001f || toVector.sqrMagnitude < 0.0001f)
            return 0.0f;

        fromVector.Normalize();
        toVector.Normalize();

        return Vector3.SignedAngle(fromVector, toVector, Vector3.up);
    }
}
