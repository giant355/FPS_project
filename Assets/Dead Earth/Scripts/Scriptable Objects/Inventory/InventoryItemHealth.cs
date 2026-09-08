using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Inventory System/Items/Health")]
public class InventoryItemHealth : InventoryItem
{
    // Inspector Assigned
    [Header("Health Properties")]
    [Tooltip("The amount Health is boosted on consumption.")]
    [Range(0.0f, 100.0f)]
    [SerializeField] protected float _healingAmount = 0.0f;

    [Header("Shared Variables")]
    [Tooltip("The SharedFloat that receives the boost.")]
    [SerializeField] protected SharedFloat _recipient = null;

    // Public Properties
    public float healingAmount { get { return _healingAmount; } }

    public override InventoryItem Use(Vector3 position, bool playAudio = true, Inventory inventory = null)
    {
        // Add health
        if (_recipient)
            _recipient.value = Mathf.Min(_recipient.value + _healingAmount, 100.0f);

        // Call base class for default sound processing
        return base.Use(position, playAudio);
    }
}
