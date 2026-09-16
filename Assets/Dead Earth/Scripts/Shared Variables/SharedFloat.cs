using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu( menuName ="Scriptable Objects/Shared Variables/Shared Float")]
public class SharedFloat : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField]
    private float _value = 0.0f;
    private float _runtimeValue = 0.0f;

    public float value
    {
        get { return this._runtimeValue; }
        set { this._runtimeValue = value;}
    }
    public void OnAfterDeserialize() { _runtimeValue = _value; }
    public void OnBeforeSerialize() { }
}
