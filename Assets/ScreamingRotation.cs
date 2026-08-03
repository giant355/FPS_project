using UnityEngine;

public class ScreamingRotation : AIStateMachineLink
{
    [SerializeField] private float angularSpeed = 45f;

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_stateMachine == null || _stateMachine.useRootRotation)
            return;

        Vector3 direction = _stateMachine.targetPosition - _stateMachine.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        _stateMachine.transform.rotation = Quaternion.RotateTowards(_stateMachine.transform.rotation, targetRotation, angularSpeed * Time.deltaTime);
    }
}