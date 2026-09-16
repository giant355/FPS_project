using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 让该类的数据可以被 Unity 序列化并显示在 Inspector 中
// 所有库存槽位信息的抽象父类
[System.Serializable]
public abstract class InventoryMountInfo
{
}

// 武器槽位的数据
[System.Serializable]
public class InventoryWeaponMountInfo : InventoryMountInfo
{
    public InventoryItemWeapon Weapon = null;

    // 武器当前的耐久度，范围为 0～100
    [Range(0.0f, 100.0f)]
    public float Condition = 100.0f;

    [Range(0, 100)]
    public int InGunRounds = 0;
}

// 弹药槽位的数据
[System.Serializable]
public class InventoryAmmoMountInfo : InventoryMountInfo
{
    // 当前槽位中保存的弹药类型
    public InventoryItemAmmo Ammo = null;

    // 当前槽位中的弹药数量
    public int Rounds = 0;
}

// 背包普通物品槽位的数据
[System.Serializable]
public class InventoryBackpackMountInfo : InventoryMountInfo
{
    // 当前背包槽位中保存的物品
    public InventoryItem Item = null;
}

// 库存系统的抽象基类
public abstract class Inventory : ScriptableObject
{
    // 根据槽位编号获得武器槽的数据
    public abstract InventoryWeaponMountInfo GetWeapon(int mountIndex);

    // 根据槽位编号获得弹药槽的数据
    public abstract InventoryAmmoMountInfo GetAmmo(int mountIndex);

    // 根据槽位编号获得背包槽的数据
    public abstract InventoryBackpackMountInfo GetBackpack(int mountIndex);

    public abstract void DropAmmoItem(int mountIndex, bool playAudio = true);
    public abstract void DropBackpackItem(int mountIndex, bool playAudio = true);
    public abstract void DropWeaponItem(int mountIndex, bool playAudio = true);
    public abstract bool UseBackpackItem(int mountIndex, bool playAudio = true);
    public abstract bool ReloadWeapon(int mountIndex, bool playAudio = true);
}
