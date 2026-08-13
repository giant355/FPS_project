using UnityEngine;

//InteractiveSound 和 InteractiveInfo 是两种不同的交互物品类型，它们都继承自 InteractiveItem
public class InteractiveItem : MonoBehaviour
{
    //射线碰到多个物体，返回优先级最高的
    [SerializeField] protected int _priority = 0;

    protected GameSceneManager _gameSceneManager = null;
    protected Collider _collider = null;

    public int priority => _priority;

    // 返回准星指向物品时显示的文字
    public virtual string GetText()
    {
        return null;
    }

    // 玩家按下交互键时执行
    /// <summary>
    /// 激活
    /// </summary>
    public virtual void Activate(CharacterManager characterManager) {}

    protected virtual void Start()
    {
        _gameSceneManager = GameSceneManager.Instance;
        _collider = GetComponent<Collider>();

        if (_gameSceneManager != null && _collider != null)
            _gameSceneManager.RegisterInteractiveItem(_collider.GetInstanceID(), this);
    }
}