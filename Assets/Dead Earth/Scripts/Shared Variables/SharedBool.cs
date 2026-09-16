using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu( menuName ="Scriptable Objects/Shared Variables/Shared Bool")]
public class SharedBool : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField]
    private bool _value = false;
    private bool _runtimeValue = false;

    public bool value
    {
        get { return this._runtimeValue; }
        set { this._runtimeValue = value;}
    }
    public void OnAfterDeserialize() { _runtimeValue = _value; }
    public void OnBeforeSerialize() { }
}
