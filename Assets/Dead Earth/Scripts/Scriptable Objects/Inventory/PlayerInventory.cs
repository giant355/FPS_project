using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Inventory System/Player Inventory")]
public class PlayerInventory : Inventory, ISerializationCallbackReceiver
{
    // 序列化字段：在 Inspector 中配置的槽位和开局物品，是运行时库存的初始模板
    [Header("Mount Configuration and Starting Items")]
    [SerializeField] protected List<InventoryWeaponMountInfo> _weaponMounts = new List<InventoryWeaponMountInfo>();
    [SerializeField] protected List<InventoryAmmoMountInfo> _ammoMounts = new List<InventoryAmmoMountInfo>();
    [SerializeField] protected List<InventoryBackpackMountInfo> _backpackMounts = new List<InventoryBackpackMountInfo>();

    [Header("Shared Variables")]
    [SerializeField] protected SharedTimedStringQueue _notificationQueue = null;

    [Header("Shared Variables - Broadcasters")]
    [SerializeField] protected SharedVector3 _playerPosition = null;
    [SerializeField] protected SharedVector3 _playerDirection = null;

    // 运行时槽位列表
    // 游戏过程中只修改这三份列表，从而避免改变 Inspector 中保存的初始配置
    protected List<InventoryWeaponMountInfo> _weapons = new List<InventoryWeaponMountInfo>();
    protected List<InventoryAmmoMountInfo> _ammo = new List<InventoryAmmoMountInfo>();
    protected List<InventoryBackpackMountInfo> _backpack = new List<InventoryBackpackMountInfo>();

    // ISerializationCallbackReceiver 接口：Unity 序列化之前的回调（当前不需要处理）
    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        // 先清空运行时列表，防止反序列化回调多次执行时出现重复数据
        _weapons.Clear();
        _ammo.Clear();
        _backpack.Clear();

        // 把 Inspector 中的武器槽位逐项深复制到运行时武器列表
        foreach (InventoryWeaponMountInfo info in _weaponMounts)
        {
            // new 会创建独立的 MountInfo 对象；直接写 clone = info 只会复制引用
            InventoryWeaponMountInfo clone = new InventoryWeaponMountInfo();
            clone.Condition = info.Condition;
            clone.InGunRounds = info.InGunRounds;
            clone.Weapon = info.Weapon;
            _weapons.Add(clone);

            // 当前实现只支持两个武器槽位，因此忽略 Inspector 中配置的其他武器
            if (_weapons.Count == 2) break;
        }

        // 把 Inspector 中的弹药槽位逐项深复制到运行时弹药列表
        foreach (InventoryAmmoMountInfo info in _ammoMounts)
        {
            InventoryAmmoMountInfo clone = new InventoryAmmoMountInfo();
            clone.Ammo = info.Ammo;
            clone.Rounds = info.Rounds;
            _ammo.Add(clone);
        }

        // 把 Inspector 中的背包槽位逐项深复制到运行时背包列表
        foreach (InventoryBackpackMountInfo info in _backpackMounts)
        {
            InventoryBackpackMountInfo clone = new InventoryBackpackMountInfo();
            clone.Item = info.Item;
            _backpack.Add(clone);
        }

    }

    public override InventoryWeaponMountInfo GetWeapon(int mountIndex)
    {
        // 当前实现只允许两个武器槽位；其他派生类可以采用不同的槽位设计
        if (mountIndex < 0 || mountIndex > 1 || mountIndex >= _weapons.Count) return null;

        // 返回指定槽位的武器信息
        return _weapons[mountIndex];
    }

    public override InventoryAmmoMountInfo GetAmmo(int mountIndex)
    {
        if (mountIndex < 0 || mountIndex >= _ammo.Count) return null;
        return _ammo[mountIndex];
    }

    public override InventoryBackpackMountInfo GetBackpack(int mountIndex)
    {
        if (mountIndex < 0 || mountIndex >= _backpack.Count) return null;
        return _backpack[mountIndex];
    }

    public override void DropAmmoItem(int mountIndex, bool playAudio = true)
    {
        Debug.Log("Ammo Dropped");
    }

    public override void DropBackpackItem(int mountIndex, bool playAudio = true)
    {
        Debug.Log("Backpack Item Dropped");
    }

    public override void DropWeaponItem(int mountIndex, bool playAudio = true)
    {
        Debug.Log("Weapon Dropped");
    }

    public override bool UseBackpackItem(int mountIndex, bool playAudio = true)
    {
        Debug.Log("Item Used");
        return false;
    }

    public override bool ReloadWeapon(int mountIndex, bool playAudio = true)
    {
        Debug.Log("Weapon Reloaded");
        return false;
    }
}
