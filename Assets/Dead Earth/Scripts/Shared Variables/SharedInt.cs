using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu( menuName ="Scriptable Objects/Shared Variables/Shared Int")]
public class SharedInt : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField]
    private int _value = 0;
    private int _runtimeValue = 0;

    public int value
    {
        get { return this._runtimeValue; }
        set { this._runtimeValue = value;}
    }
    public void OnAfterDeserialize() { _runtimeValue = _value; }
    public void OnBeforeSerialize() { }
}
