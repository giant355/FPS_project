using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}
