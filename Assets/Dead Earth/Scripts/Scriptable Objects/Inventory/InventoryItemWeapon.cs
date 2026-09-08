using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ------------------------------------------------------------------------------------------------
// CLASS    :   InventoryItemWeapon
// DESC     :   Represents a Weapon in the game and its capabilities
// ------------------------------------------------------------------------------------------------
[CreateAssetMenu(menuName = "Scriptable Objects/Inventory System/Items/Weapon")]
public class InventoryItemWeapon : InventoryItem
{
    // Inspector Assigned
    [Header("Weapon Properties")]

    [Tooltip("Is this a Single Handed or Dual Handed weapon")]
    [SerializeField] protected InventoryWeaponType _weaponType = InventoryWeaponType.SingleHanded;

    [Tooltip("Is this a Melee Weapon or requires Ammunition")]
    [SerializeField] protected InventoryWeaponFeedType _weaponFeedType = InventoryWeaponFeedType.None;

    [Tooltip("What InventoryItem object does this weapon use for Ammunition.")]
    [SerializeField] protected InventoryItemAmmo _ammo = null;

    [Tooltip("What is the Ammo Capacity of the gun.\n\nOnly required for guns that support partial reload type.")]
    [SerializeField] protected int _ammoCapacity = 0;

    [Tooltip("What is the Reload Type (Partial or Non-Partial)\n\n" +
             "Partial - Individual rounds/cartridges can be loaded into the gun one at a time. Ammo capacity is determined by ammoCapacity (above) of the weapon.\n\n" +
             "NonPartial - Reloads are done by switching magazines. Weapon ammo cpacity is determined by capacity of ammo item.")]
    [SerializeField] protected InventoryWeaponReloadType _reloadType = InventoryWeaponReloadType.None;

    [Tooltip("Max Range in meters of this weapon.")]
    //射程
    [SerializeField] protected float _range = 0.0f;

    [Tooltip("Accuracy of weapon.")]
    [Range(0, 1)]
    //精准度
    [SerializeField] protected float _accuracy = 1.0f;

    [Tooltip("Should this weapon auto-fire when FIRE button is being held in a pressed state.")]
    [SerializeField] protected bool _autoFire = false;

    [Tooltip("Does this weapon have a dual firing more (like a sniper mode)")]
    //瞄准模式
    [SerializeField] protected bool _dualMode = false;

    [Header("Damage Properties")]
    [Tooltip("Thinkness of Raycast used for raycasting potential damage. \n\n" +
             "If zero, a standard raycast is used.\nIf non-zero, a SphereCast is used with the desired radius.\n\n" +
             "Thickness can be used to emulate a blast radius like in the case of a Shotgun or Gravity Gun.")]
    //射线半径
    [SerializeField] protected float _rayRadius = 0.0f;

    [Tooltip("Maximum damage done to the Head of an enemy with a single hit.")]
    [SerializeField] protected int _headDamage = 100;

    [Tooltip("Maximum damage done to the body of an enemy with a single hit.")]
    [SerializeField] protected int _bodyDamage = 20;

    [Tooltip("How damage is diluted over the range of the weapon.")]
    [SerializeField] protected AnimationCurve _damageAttenuation = new AnimationCurve();

    [Tooltip("Force applied by this weapon on a target.")]
    [SerializeField] protected float _force = 100.0f;

    [Tooltip("How force is diluted over the range of the weapon.")]
    [SerializeField] protected AnimationCurve _forceAttenuation = new AnimationCurve();

    [Header("FPS Arms Animation Properties")]
    [Tooltip("FPS Arms Animator Sub-State Machine index to use when performing animations for this weapon.")]
    [SerializeField] protected int _weaponAnim = -1;

    // Public Properties
    public InventoryWeaponType weaponType { get { return _weaponType; } }
    public InventoryWeaponFeedType weaponFeedType { get { return _weaponFeedType; } }
    public InventoryItemAmmo ammo { get { return _ammo; } }
    public InventoryWeaponReloadType reloadType { get { return _reloadType; } }
    public float range { get { return _range; } }
    public float accuracy { get { return _accuracy; } }
    public bool autoFire { get { return _autoFire; } }
    public bool dualMode { get { return _dualMode; } }
    public int headDamage { get { return _headDamage; } }
    public int bodyDamage { get { return _bodyDamage; } }
    public float rayRadius { get { return _rayRadius; } }
    public float force { get { return _force; } }
    public int weaponAnim { get { return _weaponAnim; } }

    // 根据武器的装弹类型返回弹药容量：
    // None：近战武器或不使用弹药的武器，容量为 0；
    // Partial：武器有固定弹匣，容量由武器自身的 _ammoCapacity 决定；
    // NonPartial：武器没有固定弹匣，容量由关联弹药资源 _ammo 的 capacity 决定。
    // 如果 NonPartial 武器没有关联弹药资源，则返回 -1，表示配置无效。
    public int ammoCapacity
    {
        // Returns the correct max capcity of the weapon based on ReloadType and Ammo.
        get
        {
            switch (_reloadType)
            {
                case InventoryWeaponReloadType.None: return 0;
                case InventoryWeaponReloadType.Partial: return _ammoCapacity;
                case InventoryWeaponReloadType.NonPartial:
                    if (_ammo == null) return -1;
                    return _ammo.capacity;
            }

            return -1;
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   GetAttenuatedDamage
    // Desc :   Given a distance in meters and a body part string ("Head" or "Body") will return
    //          the damage that weapon does to that body part 
    // --------------------------------------------------------------------------------------------
    public int GetAttentuatedDamage(string bodyPart, float distance)
    {
        float normalizedDistance = Mathf.Clamp(distance / _range, 0.0f, 1.0f);
        if (bodyPart.Equals("Head"))
            return (int)(_damageAttenuation.Evaluate(normalizedDistance) * _headDamage);
        else
            return (int)(_damageAttenuation.Evaluate(normalizedDistance) * _bodyDamage);

    }

    // ---------------------------------------------------------------------------------------------
    // Name :   GetAttenuatedForce
    // Desc :   Given a distance to a target return the amount of force that will be recieved 
    //          from this weapon at this distance
    // ---------------------------------------------------------------------------------------------
    public float GetAttentuatedForce(float distance)
    {
        if (_force == 0.0f) return 0.0f;
        else
            return _forceAttenuation.Evaluate(Mathf.Clamp(distance / _range, 0.0f, 1.0f)) * _force;
    }
}
