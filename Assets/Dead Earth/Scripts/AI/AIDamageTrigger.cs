using UnityEngine;

public class AIDamageTrigger : MonoBehaviour
{
    [SerializeField] string _parameter = "";
    [SerializeField] Transform _bloodParticlesMount = null;
    [SerializeField][Range(0.01f, 1.0f)] float _bloodParticlesBurstTime = 0.1f;
    [SerializeField][Range(1, 100)] int _bloodParticlesBurstAmount = 50;
    [SerializeField] float _damageAmount = 0.1f;

    AIStateMachine _stateMachine = null;
    Animator _animator = null;
    int _parameterHash = -1;
    float _nextBloodTime = 0.0f;

    void Start()
    {
        _stateMachine = transform.root.GetComponentInChildren<AIStateMachine>();

        if (_stateMachine != null)
            _animator = _stateMachine.animator;

        _parameterHash = Animator.StringToHash(_parameter);
    }

    void OnTriggerStay(Collider other)
    {
        if (_animator == null)
            return;

        if (_bloodParticlesMount == null)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (_animator.GetFloat(_parameterHash) <= 0.9f)
            return;

        CharacterManager characterManager = other.GetComponent<CharacterManager>();
        if (characterManager != null)
            characterManager.TakeDamage(_damageAmount);

        if (Time.time < _nextBloodTime)
            return;

        if (GameSceneManager.Instance == null || GameSceneManager.Instance.bloodParticles == null)
            return;

        ParticleSystem system = GameSceneManager.Instance.bloodParticles;
        system.transform.SetPositionAndRotation(_bloodParticlesMount.position, _bloodParticlesMount.rotation);

        var main = system.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        system.Emit(_bloodParticlesBurstAmount);
        _nextBloodTime = Time.time + _bloodParticlesBurstTime;
    }
}
