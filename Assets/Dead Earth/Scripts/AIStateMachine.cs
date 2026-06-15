using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public enum AIStateType { None,Idle,Alerted,Patrol,Attack,Feeding,Pursuit,Dead}
public enum AITargetType { None,Waypoint,Visual_Player,Visual_Light,Visual_Food,Audio}
public enum AITriggerEventType { Enter,Stay,Exit}
public struct AITarget
{
    private AITargetType _AITargetType;
    private Collider _collider;
    private Vector3 _position;
    private float _distance;
    private float _time;

    public AITargetType AITargetType { get { return _AITargetType; } }
    public Collider collider { get { return _collider; } }
    public Vector3 position { get { return _position; } }
    public float distance { get { return _distance; }set { _distance = value; } }
    public float time { get { return _time; } }

    public void Set(AITargetType type,Collider collider,Vector3 position, float distance)
    {
        _AITargetType = type;
        _collider = collider;
        _position = position;
        _distance = distance;
        _time = Time.time;
    }
    public void Clear()
    {
        _AITargetType = AITargetType.None;
        _collider = null;
        _position = Vector3.zero;
        _distance = Mathf.Infinity;
        _time = 0f;
    }
}
//==================================================================================================
public abstract class AIStateMachine : MonoBehaviour
{
    public AITarget VisualThreat = new AITarget();
    public AITarget AudioThreat = new AITarget();
    protected AITarget _target = new AITarget();
    protected AIState _currentState = null;
    protected int _rootPositionRefCount = 0;
    protected int _rootRotationRefCount = 0;

    [SerializeField][Range(0, 10)] public float _stoppingDistance;
    [SerializeField] protected AIStateType _currentStateType = AIStateType.Idle;
    [SerializeField] protected SphereCollider _targetTrigger=null;
    [SerializeField] protected SphereCollider _sensorTrigger=null;

    protected Animator _animator = null;
    protected NavMeshAgent _navAgent = null;
    protected Collider _collider = null;
    protected Transform _transform = null;

    public Animator animator { get { return _animator; } }
    public NavMeshAgent navAgent { get { return _navAgent; }}

    private Dictionary<AIStateType, AIState> _states = new Dictionary<AIStateType, AIState>();
    public Vector3 sensorPosition
    {
        get
        {
            if (_sensorTrigger == null) return Vector3.zero;
            Vector3 point=_sensorTrigger.transform.position;
            point.x += _sensorTrigger.center.x * _sensorTrigger.transform.lossyScale.x;
            point.y += _sensorTrigger.center.y * _sensorTrigger.transform.lossyScale.y;
            point.z += _sensorTrigger.center.z * _sensorTrigger.transform.lossyScale.z;
            return point;
        }
    }
    public float sensorRadius
    {
        get
        {
            if (_sensorTrigger == null) return 0.0f;
            float radius = Mathf.Max(_sensorTrigger.radius * _sensorTrigger.transform.lossyScale.x,
                                     _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.y);
            return Mathf.Max(radius, _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.z);
        }
    }
    /// <summary>
    /// 当前_target的类型
    /// </summary>
	public AITargetType		targetType 	   { get { return _target.AITargetType; }}
    public Vector3 targetPosition { get { return _target.position; } }

    public bool useRootPosition { get { return _rootPositionRefCount > 0; }}
    public bool useRootRotation { get { return _rootRotationRefCount > 0; }}
    // Start is called before the first frame update
    protected virtual void Awake()
    {
        _transform = transform;
        _animator = GetComponent<Animator>();
        _collider = GetComponent<Collider>();
        _navAgent = GetComponent<NavMeshAgent>();

        if(GameSceneManager.Instance != null)
        {
            if(_collider)GameSceneManager.Instance.RegisterAIStateMachine(_collider.GetInstanceID(),this);
            if(_sensorTrigger)GameSceneManager.Instance.RegisterAIStateMachine(_sensorTrigger.GetInstanceID(),this);
            // -------------------------------------------------------------------------
            // BUG修复：原代码只注册了Collider和SensorTrigger的ID，但ColliderIsVisible中
            // 通过hit.rigidbody.GetInstanceID()查询。Collider和Rigidbody是不同的Component，
            // 其GetInstanceID()不同，导致本僵尸的身体部件永远查不到匹配，无法过滤自身遮挡。
            // 修复：额外用根Transform的ID注册，查询时用hit.transform.root.GetInstanceID()。
            // 同一僵尸层级下的所有组件共享同一个root Transform，可正确匹配。
            // -------------------------------------------------------------------------
            GameSceneManager.Instance.RegisterAIStateMachine(transform.GetInstanceID(), this);
        }
    }
    protected virtual void Start()
    {
        if(_sensorTrigger!=null)
        {
            AISensor script=_sensorTrigger.GetComponent<AISensor>();
            if (script!=null)
            {
                script.parentStateMachine= this;
            }
        }
        AIState[] states = GetComponents<AIState>();
        foreach(AIState state in states)
        {
            if(state!=null&&!_states.ContainsKey(state.GetStateType()))
            {
                _states[state.GetStateType()] = state;
                state.SetStateMachine(this);
            }
        }
        if(_states.ContainsKey(_currentStateType))
        {
            _currentState = _states[_currentStateType];
            _currentState.OnEnterState();
        }
        else
        {
            _currentState = null;
        }
        AIStateMachineLink[] scripts = _animator.GetBehaviours<AIStateMachineLink>();
        foreach(AIStateMachineLink script in scripts)
        {
            script.stateMachine= this;
        }
    }
    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d)
    {
        _target.Set(t, c, p, d);

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }
    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d,float s)
    {
        _target.Set(t, c, p, d);

        if (_targetTrigger != null)
        {
            _targetTrigger.radius =s;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }
    public void SetTarget(AITarget t)
    {
        _target = t;

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }
    /// <summary>
    /// 清空_target
    /// </summary>
    public void ClearTarget()
    {
        _target.Clear();
        if(_targetTrigger!=null)
        {
            _targetTrigger.enabled = false;
        }
    }
    protected virtual void FixedUpdate()
    {
        VisualThreat.Clear();
        AudioThreat.Clear();

        if(_target.AITargetType!=AITargetType.None)
        {
            _target.distance = Vector3.Distance(_target.position,this.transform.position);
        }
    }
    // Update is called once per frame
    protected virtual void Update()
    {
        if (_currentState == null) return;

        AIStateType newStateType = _currentState.OnUpdate();
        if(newStateType!=_currentStateType)
        {
            AIState newState = null;
            if(_states.TryGetValue(newStateType,out newState))
            {
                _currentState.OnExitState();
                newState.OnEnterState();
                _currentState = newState;
            }
            else if(_states.TryGetValue(AIStateType.Idle,out newState))
            {
                _currentState.OnExitState();
                newState.OnEnterState();
                _currentState = newState;
            }
            _currentStateType = newStateType;

        }
        print(_currentState);
    }
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (_targetTrigger == null || other != _targetTrigger||_currentState==null) return;
        _currentState.OnDestinationReached(true);
    }
    protected virtual void OnTriggerExit(Collider other)
    {
        if (_targetTrigger == null || other != _targetTrigger || _currentState == null) return;
        _currentState.OnDestinationReached(false);
    }
    public virtual void OnTriggerEvent(AITriggerEventType type,Collider other)
    {
        if (_currentState!=null)
            _currentState.OnTriggerEvent(type, other);
    }
    protected virtual void OnAnimatorMove()
    {
        if (_currentState!=null) 
            _currentState.OnAnimatorUpdated();
    }
    protected virtual void OnAnimatorIK(int layerIndex)
    {
        if (_currentState != null)
            _currentState.OnAnimatorIKUpdated();
    }
    public void NavAgentControl(bool positionUpdate,bool rorationUpdate)
    {
        if(_navAgent)
        {
            _navAgent.updatePosition = positionUpdate;
            _navAgent.updateRotation = rorationUpdate;
        }
        
    }
    public void AddRootMotionRequest(int rootPosition, int rootRotation)
    {
        _rootPositionRefCount += rootPosition;
        _rootRotationRefCount += rootRotation;
    }
    
}
