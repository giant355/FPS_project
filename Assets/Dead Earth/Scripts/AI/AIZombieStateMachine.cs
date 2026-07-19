using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AIBoneControlType { Animated , Ragdoll , RagdollToAnim}
public class BodyPartSnapshot
{
    public Transform transform = null;
    public Vector3 position = Vector3.zero;
    public Quaternion rotation = Quaternion.identity;
    public Quaternion localRotation = Quaternion.identity;
}
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

    [SerializeField] private float _reanimationBlendTime = 2.0f;
    [SerializeField] private float _reanimationWaitTime = 3.0f;
    [SerializeField] private float _mecanimTransitionTime = 0.1f;

    [SerializeField] private LayerMask _geometryLayers = -1;

    private IEnumerator _reanimationCoroutine = null;

    private int _seeking = 0;
    private int _attackType = 0;
    private float _speed = 0;
    private bool _feeding = false;
    private bool _crawling = false;

    private AIBoneControlType _boneControlType = AIBoneControlType.Animated;
    private List<BodyPartSnapshot> _bodyPartSnapShots = new List<BodyPartSnapshot>();
    private float _ragdollEndTime = float.MinValue;
    private Vector3 _ragdollHipPosition;
    private Vector3 _ragdollFeetPosition;
    private Vector3 _ragdollHeadPosition;

    private int _speedHash = Animator.StringToHash("Speed");
    private int _seekingHash = Animator.StringToHash("Seeking");
    private int _feedingHash = Animator.StringToHash("Feeding");
    private int _attackHash = Animator.StringToHash("Attack");
    private int _crawlHash = Animator.StringToHash("Crawling");
    private int _hitTypeHash = Animator.StringToHash("HitType");
    private int _hitTriggerHash = Animator.StringToHash("Hit");
    private int _lowerBodyDamageHash = Animator.StringToHash("Lower Body Damage");
    private int _upperBodyDamageHash = Animator.StringToHash("Upper Body Damage");
    private int _reanimateFromBackHash = Animator.StringToHash("Reanimate From Back");
    private int _reanimateFromFrontHash = Animator.StringToHash("Reanimate From Front");
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

        if(_rootBone != null)
        {
            Transform[] transforms = _rootBone.GetComponentsInChildren<Transform>();
            foreach(Transform trans in transforms)
            {
                BodyPartSnapshot snapShot = new BodyPartSnapshot();
                snapShot.transform = trans;
                _bodyPartSnapShots.Add(snapShot);
            }
        }

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
            _crawling = isCrawling;

            _animator.SetInteger(_lowerBodyDamageHash, _lowerBodyDamage);
            _animator.SetInteger(_upperBodyDamageHash, _upperBodyDamage);
            _animator.SetBool(_crawlHash, _crawling);
        }
    }
    private void StartReanimation()
    {
        //先清空，因为有可能在此期间玩家又对僵尸开枪，需要停掉旧协程，开启新协程
        if (_reanimationCoroutine != null)
        {
            StopCoroutine(_reanimationCoroutine);
        }

        _reanimationCoroutine = Reanimate();
        StartCoroutine(_reanimationCoroutine);
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
                    StartReanimation();
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
                if (angle < -20 || hitDirection == -1) hitType = 1;
                else
                if (angle > 20 || hitDirection == 1)   hitType = 3;
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
            //print("HITTYPE: " + hitType);
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
                StartReanimation();
            }
        }

    }
    private IEnumerator Reanimate()
    {
        //检查现在能不能复活
        //等 ragdoll 稳定下来
        //关闭物理控制
        //记录 ragdoll 最后一刻的骨骼姿势
        //重新打开 Animator
        //判断趴着 / 仰着，触发对应动画

        //拦截错误状态
        if (_boneControlType != AIBoneControlType.Ragdoll || _animator == null || _rootBone == null)
        {
            yield break;
        }

        //等几秒让ragdoll稳定
        yield return new WaitForSeconds(_reanimationWaitTime);

        _ragdollEndTime = Time.time;

        //让刚体不再受物理系统控制
        foreach (Rigidbody body in _bodyParts)
        {
            if (body != null)
            {
                body.isKinematic = true;
            }
        }

        _boneControlType = AIBoneControlType.RagdollToAnim;

        foreach (BodyPartSnapshot snapshot in _bodyPartSnapShots)
        {
            snapshot.position = snapshot.transform.position;
            snapshot.rotation = snapshot.transform.rotation;
            snapshot.localRotation = snapshot.transform.localRotation;
        }

        _ragdollHipPosition = _rootBone.position;
        _ragdollHeadPosition = _animator.GetBoneTransform(HumanBodyBones.Head).position;

        Vector3 leftFootPosition = _animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
        Vector3 rightFootPosition = _animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
        _ragdollFeetPosition = (leftFootPosition + rightFootPosition) * 0.5f;

        _animator.enabled = true;

        Vector3 rootBoneForward = _rootBone.forward;

        if (_rootBoneAlignment == AIBoneAlignmentType.XAxis)
            rootBoneForward = _rootBone.right;
        else if (_rootBoneAlignment == AIBoneAlignmentType.XAxisInverted)
            rootBoneForward = -_rootBone.right;
        else if (_rootBoneAlignment == AIBoneAlignmentType.YAxis)
            rootBoneForward = _rootBone.up;
        else if (_rootBoneAlignment == AIBoneAlignmentType.YAxisInverted)
            rootBoneForward = -_rootBone.up;
        else if (_rootBoneAlignment == AIBoneAlignmentType.ZAxisInverted)
            rootBoneForward = -_rootBone.forward;

        if (rootBoneForward.y > 0.0f)
        {
            _animator.SetTrigger(_reanimateFromBackHash);
        }
        else
        {
            _animator.SetTrigger(_reanimateFromFrontHash);
        }

        //先要等几秒再置空
        _reanimationCoroutine = null;
    }

    //LateUpdate在渲染开始前
    public virtual void LateUpdate()
    {
        if (_boneControlType != AIBoneControlType.RagdollToAnim)
            return;

        if (Time.time <= _ragdollEndTime + _mecanimTransitionTime)
        {
            print("SUCCESS");
            Vector3 animatedToRagdoll = _ragdollHipPosition - _rootBone.position;
            Vector3 newRootPosition = transform.position + animatedToRagdoll;

            //从角色目标位置上方 0.25 米往下打一条射线，找地面
            RaycastHit[] hits = Physics.RaycastAll(newRootPosition + Vector3.up * 0.25f,Vector3.down,10.0f,_geometryLayers);

            newRootPosition.y = -10000;
            //如果射线打到地板、箱子、台阶，就选择最高的那个表面
            foreach (RaycastHit hit in hits)
            {
                newRootPosition.y = Mathf.Max(newRootPosition.y, hit.point.y);
            }

            NavMeshHit navMeshHit;
            if (NavMesh.SamplePosition(newRootPosition, out navMeshHit, 3.0f, NavMesh.AllAreas))
            {
                Vector3 correctedPosition = navMeshHit.position + Vector3.up * _navAgent.baseOffset;
                print("_navAgent.baseOffset" + _navAgent.baseOffset);
                transform.position = correctedPosition;
            }
            else
            {
                newRootPosition.y = 0.01f + _navAgent.baseOffset;
                transform.position = newRootPosition;
            }
            
            Vector3 ragdollDirection = _ragdollHeadPosition - _ragdollFeetPosition;
            ragdollDirection.y = 0.0f;

            Vector3 animatedHeadPosition = _animator.GetBoneTransform(HumanBodyBones.Head).position;
            Vector3 animatedLeftFootPosition = _animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            Vector3 animatedRightFootPosition = _animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
            Vector3 animatedFeetPosition = (animatedLeftFootPosition + animatedRightFootPosition) * 0.5f;

            Vector3 animatedDirection = animatedHeadPosition - animatedFeetPosition;
            animatedDirection.y = 0.0f;

            if (ragdollDirection.sqrMagnitude > 0.0001f && animatedDirection.sqrMagnitude > 0.0001f)
            {
                float angle = Vector3.SignedAngle(animatedDirection, ragdollDirection, Vector3.up);
                transform.rotation *= Quaternion.AngleAxis(angle, Vector3.up);
            }
        }
       
        float blendAmount = Mathf.Clamp01((Time.time - _ragdollEndTime - _mecanimTransitionTime) / _reanimationBlendTime);

        foreach (BodyPartSnapshot snapshot in _bodyPartSnapShots)
        {
            if (snapshot.transform == _rootBone)
            {
                snapshot.transform.position = Vector3.Lerp(snapshot.position, snapshot.transform.position, blendAmount);
                snapshot.transform.rotation = Quaternion.Slerp(snapshot.rotation, snapshot.transform.rotation, blendAmount);
            }
            else
            {
                snapshot.transform.localRotation = Quaternion.Slerp(
                    snapshot.localRotation,
                    snapshot.transform.localRotation,
                    blendAmount
                );
            }
        }

        if (blendAmount >= 1.0f)
        {
            _boneControlType = AIBoneControlType.Animated;

            if (_navAgent != null)
                _navAgent.enabled = true;

            if (_collider != null)
                _collider.enabled = true;

            AIZombieState alertedState = GetComponent<AIZombieState_Alerted1>();
            if (alertedState != null)
            {
                _currentState = alertedState;
                _currentStateType = AIStateType.Alerted;
                _currentState.OnEnterState();
            }
        }
       
    }
}

