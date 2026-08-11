using UnityEngine;

public class AIDamageTrigger : MonoBehaviour
{
    [SerializeField] string _parameter = "";
    [SerializeField] Transform _bloodParticlesMount = null;
    [SerializeField][Range(0.01f, 1.0f)] float _bloodParticlesBurstTime = 0.1f;
    [SerializeField][Range(1, 100)] int _bloodParticlesBurstAmount = 50;
    [SerializeField] float _damageAmount = 0.1f;

    [SerializeField] bool _doDamageSound = true;
    [SerializeField] bool _doPainSound = true;

    AIStateMachine _stateMachine = null;
    Animator _animator = null;
    int _parameterHash = -1;
    float _nextBloodTime = 0.0f;

    bool _firstContact = false;//记录攻击是否刚接触玩家，防止 OnTriggerStay 每个物理帧都播放撞击声。

    void Start()
    {
        _stateMachine = transform.root.GetComponentInChildren<AIStateMachine>();

        if (_stateMachine != null)
            _animator = _stateMachine.animator;

        _parameterHash = Animator.StringToHash(_parameter);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_animator == null) return;
        if (other.CompareTag("Player") && _animator.GetFloat(_parameterHash) > 0.9f) _firstContact = true;
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
        {
            characterManager.TakeDamage(_damageAmount, _doDamageSound && _firstContact, _doPainSound);
            _firstContact = false;
        }

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
