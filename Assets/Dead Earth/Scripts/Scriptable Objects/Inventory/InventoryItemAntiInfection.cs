using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Inventory System/Items/Anti-Infection")]
public class InventoryItemAntiInfection : InventoryItem
{
    // Inspector Assigned
    [Header("Anti-Infection Properties")]
    [Tooltip("The amount Infection is reduced on consumption.")]
    [Range(0.0f, 100.0f)]
    [SerializeField] protected float _reductionAmount = 0.0f;

    [Header("Shared Variables")]
    [Tooltip("The SharedFloat that receives the Reduction.")]
    [SerializeField] protected SharedFloat _recipient = null;

    // Public Properties
    public float reductionAmount { get { return _reductionAmount; } }

    public override InventoryItem Use(Vector3 position, bool playAudio = true, Inventory inventory = null)
    {   
        // Add health
        if (_recipient)
            _recipient.value = Mathf.Max(_recipient.value - _reductionAmount, 0.0f);

        // Call base class for default sound processing
        return base.Use(position, playAudio);
    }
}
