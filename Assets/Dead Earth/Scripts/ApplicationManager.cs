using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class GameState
{
    public string Key = null;
    public string Value = null;
}

//全局管理游戏任务状态的单例类
[DefaultExecutionOrder(-100)]
public class ApplicationManager : MonoBehaviour
{

    [SerializeField] private List<GameState> _startingGameStates = new List<GameState>();

    private Dictionary<string, string> _gameStateDictionary = new Dictionary<string, string>();

    private static ApplicationManager _instance = null;

    public static ApplicationManager Instance => _instance;

    void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if(_instance != this)
        {
            Destroy(gameObject);
            //destory不会立即销毁，Awake会被调用两次，所以这里直接return
            return;
        }

        for (int i = 0; i < _startingGameStates.Count; i++)
        {
            GameState gs = _startingGameStates[i];
            //把inspector里面设置的初始状态搬入私有字典
            _gameStateDictionary[gs.Key] = gs.Value;
        }
    }

    public string GetGameState(string key)
    {
        string result = null;
        _gameStateDictionary.TryGetValue(key, out result);
        return result;
    }

    public bool SetGameState(string key, string value)
    {
        if (key == null || value == null) return false;

        _gameStateDictionary[key] = value;
        return true;
    }

    /// <summary>
    /// 检查指定的前置游戏状态是否全部满足。
    /// </summary>
    /// <param name="requiredStates">
    /// 需要满足的状态列表；列表为空或为 null 时，视为没有前置条件。
    /// </param>
    /// <returns>
    /// 当所有状态的键和值均与当前游戏状态一致时返回 true；
    /// 任一状态缺失或值不匹配时返回 false。
    /// </returns>
    public bool AreStatesSet(List<GameState> requiredStates)
    {
        if (requiredStates == null || requiredStates.Count == 0)
            return true;

        foreach (GameState requiredState in requiredStates)
        {
            if (GetGameState(requiredState.Key) != requiredState.Value)
                return false;
        }

        return true;
    }
}
