using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AIBoneControlType { Animated , Ragdoll , RagdollToAnim}
public class AIZombieStateMachine :AIStateMachine
{
    [SerializeField][Range(10.0f, 360.0f)] float _fov = 50.0f;
    [SerializeField][Range(0.0f,1.0f)] float _sight = 0.5f;
    [SerializeField][Range(0.0f, 1.0f)] float _aggression = 0.5f;
    [SerializeField][Range(0.0f, 1.0f)] float _intelligence = 0.5f;
    [SerializeField][Range(0.0f, 1.0f)] float _hearing = 1f;
    [SerializeField][Range(0.0f, 1.0f)] float _satisfaction = 1f;
    [SerializeField][Range(0, 100)] int _health = 100;

    [SerializeField][Range(0, 100)] int _lowerBodyDamage;
    [SerializeField][Range(0, 100)] int _upperBodyDamage;
    [SerializeField][Range(0, 100)] int _upperBodyThreshold = 30;
    [SerializeField][Range(0, 100)] int _limpThreshold = 30;
    [SerializeField][Range(0, 100)] int _crawlThreshold = 90;

    [SerializeField] float _replenishRate = 0.1f;
    [SerializeField] float _depletionRate = 0.01f;

    private int _seeking = 0;
    private int _attackType = 0;
    private float _speed = 0;
    private bool _feeding = false;
    private bool _crawling = false;

    private AIBoneControlType _boneControlType = AIBoneControlType.Animated;

    private int _speedHash = Animator.StringToHash("Speed");
    private int _seekingHash = Animator.StringToHash("Seeking");
    private int _feedingHash = Animator.StringToHash("Feeding");
    private int _attackHash = Animator.StringToHash("Attack");
    private int _crawlHash = Animator.StringToHash("Crawling");
    private int _hitTypeHash = Animator.StringToHash("HitType");
    private int _hitTriggerHash = Animator.StringToHash("Hit");
    public float fov { get { return _fov; } }
    public float hearing { get { return _hearing; } }
    public float sight { get { return _sight; } }
    public bool crawling { get { return _crawling; } }
    public float intelligence { get { return _intelligence; } }
    public float satisfaction { get { return _satisfaction; } set { _satisfaction = value; } }
    public float aggression { get { return _aggression; } set { _aggression = value; } }
    public int health { get { return _health; } set { _health = value; } }
    public int attackType { get { return _attackType; } set { _attackType = value; } }
    public bool feeding { get { return _feeding; } set { _feeding = value; } }
    public int seeking { get { return _seeking; } set { _seeking = value; } }
    public float replenishRate { get { return _replenishRate; } }
    public float depletionRate { get { return _depletionRate; } }

    /// <summary>
    /// 在animator面板设置
    /// </summary>
    public float speed
    {
        get { return _speed; }
        set { _speed = value; }
    }
    public bool isCrawling
    {
        get { return (_lowerBodyDamage >= _crawlThreshold); }
    }
    protected override void Start()
    {
        base.Start();
        UpdateAnimatorDamage();
    }
    protected override void Update()
    {
        base.Update();
        //print("VisualThreat:"+VisualThreat.AITargetType);
        if (_animator != null)
        {
            _animator.SetFloat(_speedHash, _speed);
            _animator.SetBool(_feedingHash, _feeding);
            _animator.SetInteger(_seekingHash, _seeking);
            _animator.SetInteger(_attackHash, _attackType);
        }

        _satisfaction = Mathf.Max(0,_satisfaction -  depletionRate * Time.deltaTime * Mathf.Pow(_speed,3));
    }
    /// <summary>
    /// 更新僵尸伤害相关
    /// </summary>
    protected void UpdateAnimatorDamage()
    {
        if (_animator != null)
        {
            //只要lowerBodyDamage>crawlThreshold->爬行
            _animator.SetBool(_crawlHash, isCrawling);
        }
    }

    // TakeDamage
    //  ├─ 如果已经 Ragdoll
    //  │   ├─ 继续受伤
    //  │   ├─ 继续受物理冲击
    //  │   └─ return
    //  ├─ 如果还在 Animated
    //  │   ├─ 计算伤害
    //  │   ├─ 判断是否应该 Ragdoll
    //  │   ├─ 不 Ragdoll → 播 Hit 受击动画
    //  │   └─ Ragdoll → 关闭 AI/Animator，打开物理刚体
    public override void TakeDamage(Vector3 position, Vector3 force, int damage, Rigidbody bodyPart, CharacterManager characterManager, int hitDirection = 0)
    {
        if (GameSceneManager.Instance != null && GameSceneManager.Instance.bloodParticles != null)
        {
            ParticleSystem sys = GameSceneManager.Instance.bloodParticles;
            sys.transform.position = position;
            var settings = sys.main;
            settings.simulationSpace = ParticleSystemSimulationSpace.World;
            sys.Emit(60);
        }

        float hitStrength = force.magnitude;

        if (_boneControlType==AIBoneControlType.Ragdoll)
        {
            if(bodyPart != null)
            {
                if(hitStrength > 1)
                {
                    bodyPart.AddForce(force,ForceMode.Impulse);
                }
                if(bodyPart.CompareTag("Head"))
                {
                    _health = Mathf.Max(health - damage, 0);
                }
                else if(bodyPart.CompareTag("Upper Body"))
                {
                    _upperBodyDamage += damage;
                }
                else if(bodyPart.CompareTag("Lower Body"))
                {
                    _lowerBodyDamage += damage;
                }

                UpdateAnimatorDamage();

                if(_health>0)
                {
                    //reanimation
                }
            }
            return;
        }

        Vector3 attackerLocPos = transform.InverseTransformPoint(characterManager.transform.position);

        Vector3 hitLocPos = transform.InverseTransformPoint(position);

        bool shouldRagdoll = (hitStrength > 1.0f);

        if (bodyPart != null)
        {
            if (bodyPart.CompareTag("Head"))
            {
                _health = Mathf.Max(health - damage, 0);
                if (health == 0) shouldRagdoll = true;
            }
            else if (bodyPart.CompareTag("Upper Body"))
            {
                _upperBodyDamage += damage;
                UpdateAnimatorDamage();
            }
            else if (bodyPart.CompareTag("Lower Body"))
            {
                _lowerBodyDamage += damage;
                UpdateAnimatorDamage();
                shouldRagdoll = true;
            }

        }

        if(_boneControlType != AIBoneControlType.Animated || isCrawling || cinematicEnabled || attackerLocPos.z<0)
        {
            shouldRagdoll = true;
        }
        if(!shouldRagdoll)
        {
            float angle = 0.0f;
            if (hitDirection == 0)
            {
                Vector3 vecToHit = (position - transform.position).normalized;
                angle = AIState.FindSignedAngle(vecToHit, transform.forward);
            }

            int hitType = 0;
            if (bodyPart.gameObject.CompareTag("Head"))
            {
                if (angle < -10 || hitDirection == -1) hitType = 1;
                else
                if (angle > 10 || hitDirection == 1)   hitType = 3;
                else
                                                       hitType = 2;
            }
            else
            if (bodyPart.gameObject.CompareTag("Upper Body"))
            {
                if (angle < -20 || hitDirection == -1) hitType = 4;
                else
                if (angle > 20 || hitDirection == 1)   hitType = 6;
                else
                                                       hitType = 5;
            }

            if (_animator)
            {
                _animator.SetInteger(_hitTypeHash, hitType);
                _animator.SetTrigger(_hitTriggerHash);
            }

            return;
        }
        else
        {
            if (_currentState)
            {
                _currentState.OnExitState();
                _currentState = null;
                _currentStateType = AIStateType.None;
            }
            if (_navAgent) _navAgent.enabled = false;
            if (_animator) _animator.enabled = false;
            if (_collider) _collider.enabled = false;

            inMeleeRange = false;

            foreach (Rigidbody body in _bodyParts)
            {
                if (body)
                {
                    body.isKinematic = false;
                }
            }

            if (hitStrength > 1.0f)
                bodyPart.AddForce(force, ForceMode.Impulse);

            _boneControlType = AIBoneControlType.Ragdoll;

            if(health>0)
            {
                //reanimate
            }
        }

    }
}

