using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo
{
    public Collider collider = null;
    public CharacterManager characterManager = null;
    public Camera camera = null;
    public CapsuleCollider meleeTrigger = null;
}

[DefaultExecutionOrder(-100)]
public class GameSceneManager : MonoBehaviour
{
    [SerializeField] private ParticleSystem _bloodParticles = null;

    private static GameSceneManager _instance = null;
    public static GameSceneManager Instance => _instance;
    public ParticleSystem bloodParticles {  get { return _bloodParticles; } }
    public void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if(_instance != this)
        {
            Destroy(gameObject);    
        }
    }
    public void OnDestroy()
    {
        if(_instance==this)
        {
            _instance = null;
        }
    }
    private Dictionary<int,AIStateMachine> _stateMachines = new Dictionary<int,AIStateMachine>();
    private Dictionary<int, PlayerInfo> _playerInfos = new Dictionary<int, PlayerInfo>();
    private Dictionary<int, InteractiveItem> _interactiveItems = new Dictionary<int, InteractiveItem>();

    public void RegisterAIStateMachine(int key,AIStateMachine stateMachine)
    {
        if(!_stateMachines.ContainsKey(key))
        {
            _stateMachines[key] = stateMachine;
        }
    }
    public AIStateMachine GetAIStateMachine(int key)
    {
        if( _stateMachines.ContainsKey(key))
            return _stateMachines[key];
        return null;
    }

    public void RegisterPlayerInfo(int key, PlayerInfo playerInfo)
    {
        if (!_playerInfos.ContainsKey(key))
        {
            _playerInfos[key] = playerInfo;
        }
    }

    public PlayerInfo GetPlayerInfo(int key)
    {
        PlayerInfo info = null;
        if (_playerInfos.TryGetValue(key, out info))
        {
            return info;
        }

        return null;
    }

    public void RegisterInteractiveItem(int key, InteractiveItem interactiveItem)
    {
        if (!_interactiveItems.ContainsKey(key))
            _interactiveItems[key] = interactiveItem;
    }

    public InteractiveItem GetInteractiveItem(int key)
    {
        _interactiveItems.TryGetValue(key, out InteractiveItem item);
        return item;
    }
}
