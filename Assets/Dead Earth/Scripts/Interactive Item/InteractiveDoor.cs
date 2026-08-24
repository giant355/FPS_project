using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InteractiveDoorAxisAlignment
{
    XAxis,
    YAxis,
    ZAxis
}

[System.Serializable]
public class InteractiveDoorInfo
{
    // 需要移动或旋转的门板。
    public Transform Transform = null;

    // 打开时需要增加的局部旋转角度。
    public Vector3 Rotation = Vector3.zero;

    // 打开时需要移动的局部距离。
    public Vector3 Movement = Vector3.zero;

    // 以下数据在游戏开始时计算，不需要在 Inspector 中填写。

    [HideInInspector]
    public Quaternion ClosedRotation = Quaternion.identity;

    [HideInInspector]
    public Quaternion OpenRotation = Quaternion.identity;

    [HideInInspector]
    public Vector3 OpenPosition = Vector3.zero;

    [HideInInspector]
    public Vector3 ClosedPosition = Vector3.zero;
}

[RequireComponent(typeof(BoxCollider))]
public class InteractiveDoor : InteractiveItem
{
    [Header("激活设置")]
    [Tooltip("游戏开始时，门是否处于关闭状态")]
    [SerializeField] protected bool _isClosed = true;

    [Tooltip("是否允许门根据玩家所在位置朝两个方向打开")]
    [SerializeField] protected bool _isTwoWay = true;

    [Tooltip("玩家进入门的触发区域时，门是否自动打开")]
    [SerializeField] protected bool _autoOpen = false;

    [Tooltip("门打开一段时间后，是否自动关闭")]
    [SerializeField] protected bool _autoClose = false;

    [Tooltip("自动关门的随机延迟范围：X 是最短时间，Y 是最长时间")]
    [SerializeField] protected Vector2 _autoCloseDelay = new Vector2(5.0f, 5.0f);

    [Tooltip("门打开时，交互碰撞器沿门洞前后方向扩大的倍数")]
    [SerializeField] protected float _colliderLengthOpenScale = 3.0f;

    [Tooltip("门打开时，是否将交互碰撞器的中心偏移到门的一侧")]
    [SerializeField] protected bool _offsetCollider = true;

    [Tooltip("用于存放门后或容器内部物品的父物体")]
    [SerializeField] protected Transform _contentsMount = null;

    [Tooltip("父物体的哪个局部坐标轴代表门洞的前方")]
    [SerializeField] protected InteractiveDoorAxisAlignment _localForwardAxis = InteractiveDoorAxisAlignment.ZAxis;


    [Header("游戏状态管理")]
    [Tooltip("打开门之前必须满足的游戏状态")]
    [SerializeField] protected List<GameState> _requiredStates = new List<GameState>();

    [Tooltip("打开门之前玩家必须拥有的物品 ID")]
    [SerializeField] protected List<string> _requiredItems = new List<string>();


    [Header("交互提示文本")]
    [Tooltip("门已打开时显示的提示")]
    [TextArea(3, 10)]
    [SerializeField] protected string _openedHintText = "Door: Press 'Use' to close";

    [Tooltip("门已关闭并且可以打开时显示的提示")]
    [TextArea(3, 10)]
    [SerializeField] protected string _closedHintText = "Door: Press 'Use' to open";

    [Tooltip("玩家不满足开门条件时显示的提示")]
    [TextArea(3, 10)]
    [SerializeField] protected string _cantActivateHintText = "Door: It's locked";


    [Header("门板设置")]

    [Tooltip("需要由这个控制器移动或旋转的所有子门板")]
    [SerializeField] protected List<InteractiveDoorInfo> _doors = new List<InteractiveDoorInfo>();

    // 当前正在执行的开门或关门协程。
    protected IEnumerator _coroutine = null;

    // 门关闭时交互碰撞器的尺寸和中心。
    protected Vector3 _closedColliderSize = Vector3.zero;
    protected Vector3 _closedColliderCenter = Vector3.zero;

    // 门打开时交互碰撞器的尺寸和中心。
    protected Vector3 _openColliderSize = Vector3.zero;
    protected Vector3 _openColliderCenter = Vector3.zero;

    // 当前物体上的交互碰撞器。
    protected BoxCollider _boxCollider = null;

    // 用于判断玩家位于门正面还是背面的平面。
    protected Plane _plane;

    // 记录本次开门所使用的方向，保证动画中途反向时不会突然切到门的另一侧。
    protected bool _openedFrontside = true;

    // 当前开关门动画的标准化进度，范围通常是 0～1。
    protected float _normalizedTime = 0.0f;

    public override string GetText()
    {
        //门关闭？
        //├── 是
        //│   ├── 缺少状态或物品 → "Door: It's locked"
        //│   └── 条件满足      → "Door: Press 'Use' to open"
        //└── 否               → "Door: Press 'Use' to close"
        bool haveInventoryItems = HaveRequiredInvItems();
        bool haveRequiredStates = true;

        if (_requiredStates.Count > 0)
        {
            if (ApplicationManager.Instance == null) haveRequiredStates = false;
            else haveRequiredStates = ApplicationManager.Instance.AreStatesSet(_requiredStates);
        }

        if (_isClosed)
        {
            if (!haveRequiredStates || !haveInventoryItems) return _cantActivateHintText;
            return _closedHintText;
        }

        return _openedHintText;
    }

    protected bool HaveRequiredInvItems()
    {
        return true;
    }

    protected override void Start()
    {
        base.Start();

        _boxCollider = _collider as BoxCollider;

        if (_boxCollider != null)
        {
            _closedColliderSize = _openColliderSize = _boxCollider.size;
            _closedColliderCenter = _openColliderCenter = _boxCollider.center;

            float offset = 0.0f;

            switch (_localForwardAxis)
            {
                case InteractiveDoorAxisAlignment.XAxis:
                    _plane = new Plane(transform.right, transform.position);
                    _openColliderSize.x *= _colliderLengthOpenScale;
                    offset = _closedColliderCenter.x - (_openColliderSize.x / 2.0f);
                    _openColliderCenter = new Vector3(offset, _closedColliderCenter.y, _closedColliderCenter.z);
                    break;

                case InteractiveDoorAxisAlignment.YAxis:
                    _plane = new Plane(transform.up, transform.position);
                    _openColliderSize.y *= _colliderLengthOpenScale;
                    offset = _closedColliderCenter.y - (_openColliderSize.y / 2.0f);
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, offset, _closedColliderCenter.z);
                    break;

                case InteractiveDoorAxisAlignment.ZAxis:
                    _plane = new Plane(transform.forward, transform.position);
                    _openColliderSize.z *= _colliderLengthOpenScale;
                    offset = _closedColliderCenter.z - (_openColliderSize.z / 2.0f);
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, _closedColliderCenter.y, offset);
                    break;
            }

            if (!_isClosed)
            {
                _boxCollider.size = _openColliderSize;

                if (_offsetCollider) _boxCollider.center = _openColliderCenter;
                _openedFrontside = true;
            }
        }

        foreach (InteractiveDoorInfo door in _doors)
        {
            if (door != null && door.Transform != null)
            {
                door.ClosedRotation = door.Transform.localRotation;
                door.ClosedPosition = door.Transform.position;

                door.OpenPosition = door.Transform.position - door.Transform.TransformDirection(door.Movement);

                Quaternion rotationToOpen = Quaternion.Euler(door.Rotation);

                if (!_isClosed)
                {
                    door.Transform.localRotation = door.ClosedRotation * rotationToOpen;
                    door.Transform.position = door.OpenPosition;
                }
            }
        }

        if (_contentsMount != null)
        {
            Collider[] colliders = _contentsMount.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                if (_isClosed)
                    col.enabled = false;
                else
                    col.enabled = true;
            }
        }

        _coroutine = null;
    }

    public override void Activate(CharacterManager characterManager)
    {
        bool haveRequiredStates = true;
        if (_requiredStates.Count > 0)
        {
            if (ApplicationManager.Instance == null) haveRequiredStates = false;

            haveRequiredStates = ApplicationManager.Instance.AreStatesSet(_requiredStates);
        }

        if (haveRequiredStates && HaveRequiredInvItems())
        {
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = Activate(_plane.GetSide(characterManager.transform.position));
            StartCoroutine(_coroutine);
        }
    }

    private IEnumerator Activate(bool frontSide, bool autoClosing = false, float delay = 0.0f)
    {
        yield return new WaitForSeconds(delay);

        float duration = 1.5f;
        float time = 0.0f;

        // 单向门始终使用同一个开启方向。
        if (!_isTwoWay) frontSide = true;

        if (_normalizedTime > 0.0f)
            _normalizedTime = 1 - _normalizedTime;

        if (_isClosed)
        {
            _isClosed = false;

            // 如果这是从关门动画中途反向开门，继续使用最初的开门方向。
            if (_normalizedTime > 0.0f)
                frontSide = _openedFrontside;

            _openedFrontside = frontSide;

            float offset = 0.0f;
            switch (_localForwardAxis)
            {
                case InteractiveDoorAxisAlignment.XAxis:
                    offset = _openColliderSize.x / 2.0f;
                    if (!frontSide) offset = -offset;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x - offset, _closedColliderCenter.y, _closedColliderCenter.z);
                    break;

                case InteractiveDoorAxisAlignment.YAxis:
                    offset = _openColliderSize.y / 2.0f;
                    if (!frontSide) offset = -offset;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, _closedColliderCenter.y - offset, _closedColliderCenter.z);
                    break;

                case InteractiveDoorAxisAlignment.ZAxis:
                    offset = _openColliderSize.z / 2.0f;
                    if (!frontSide) offset = -offset;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, _closedColliderCenter.y, _closedColliderCenter.z - offset);
                    break;
            }

            if (_offsetCollider) _boxCollider.center = _openColliderCenter;
            _boxCollider.size = _openColliderSize;

            time = duration * _normalizedTime;

            while (time <= duration)
            {
                foreach (InteractiveDoorInfo door in _doors)
                {
                    if (door != null && door.Transform != null)
                    {
                        _normalizedTime = time / duration;

                        door.Transform.position = Vector3.Lerp(door.ClosedPosition, door.OpenPosition, _normalizedTime);
                        door.Transform.localRotation = door.ClosedRotation * Quaternion.Euler(frontSide ? door.Rotation * _normalizedTime : -door.Rotation * _normalizedTime);
                    }
                }
                yield return null;
                time += Time.deltaTime;
            }


            if (_contentsMount != null)
            {
                Collider[] colliders = _contentsMount.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    col.enabled = true;
                }
            }

            _normalizedTime = 0.0f;

            if (_autoClose)
            {
                _coroutine = Activate(frontSide, true, Random.Range(_autoCloseDelay.x, _autoCloseDelay.y));
                StartCoroutine(_coroutine);
            }
            yield break;
        }

        else
        {
            _isClosed = true;

            foreach (InteractiveDoorInfo door in _doors)
            {
                if (door != null && door.Transform != null)
                {
                    // 始终使用完整的开门旋转作为插值起点。若直接缓存当前的半开旋转，
                    // 中途切换为关门时会对半开角度再次插值，从而产生明显跳变。
                    Quaternion rotationToOpen = Quaternion.Euler(
                        _openedFrontside ? door.Rotation : -door.Rotation);
                    door.OpenRotation = door.ClosedRotation * rotationToOpen;
                }
            }

            if (_contentsMount != null)
            {
                Collider[] colliders = _contentsMount.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    col.enabled = false;
                }
            }

            time = duration * _normalizedTime;

            while (time <= duration)
            {
                foreach (InteractiveDoorInfo door in _doors)
                {
                    if (door != null && door.Transform != null)
                    {
                        _normalizedTime = time / duration;
                        door.Transform.position = Vector3.Lerp(door.OpenPosition, door.ClosedPosition, _normalizedTime);
                        door.Transform.localRotation = Quaternion.Lerp(door.OpenRotation, door.ClosedRotation, _normalizedTime);
                    }
                }
                yield return null;
                time += Time.deltaTime;
            }

            foreach (InteractiveDoorInfo door in _doors)
            {
                if (door != null && door.Transform != null)
                {
                    door.Transform.localRotation = door.ClosedRotation;
                    door.Transform.position = door.ClosedPosition;
                }
            }


            _boxCollider.size = _closedColliderSize;
            _boxCollider.center = _closedColliderCenter;
        }

        _normalizedTime = 0.0f;
        _coroutine = null;
        yield break;
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (!_autoOpen || !_isClosed) return;
        bool haveRequiredStates = true;
        if (_requiredStates.Count > 0)
        {
            if (ApplicationManager.Instance == null) haveRequiredStates = false;
            else
                haveRequiredStates = ApplicationManager.Instance.AreStatesSet(_requiredStates);
        }

        // Only activate the door if we meet all reuirements
        if (haveRequiredStates && HaveRequiredInvItems())
        {
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = Activate(_plane.GetSide(other.transform.position));
            StartCoroutine(_coroutine);
        }

    }
}
