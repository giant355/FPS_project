using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Inventory System/Items/Ammunition")]
public class InventoryItemAmmo : InventoryItem
{
    // Inspector Assigned
    [Header("Ammo Properties")]
    [Tooltip("The maximum number of rounds/cartridges an item of this type can hold.")]
    //这种弹药物品最多能装多少发子弹
    [SerializeField] protected int _capacity = 0;

    // Public Properties
    public int capacity { get { return _capacity; } }
}
