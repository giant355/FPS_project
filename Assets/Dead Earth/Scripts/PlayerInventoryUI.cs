using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryPanelType { None, Backpack, AmmoBelt, Weapons, PDA }

[System.Serializable]
public struct InventoryUI_PDAReferences
{
    public Transform _logEntries;
    public RawImage _pDAImage;
    public Text _pDAAuthor;
    public Text _pDASubject;
    public Slider _timelineSlider;
    public Toggle _autoplayOnPickup;
    public GameObject _logEntryPrefab;
}

[System.Serializable]
public struct InventoryUI_Status
{
    public Slider HealthSlider;
    public Slider InfectionSlider;
    public Slider StaminaSlider;
    public Slider FlashlightSlider;
    public Slider NightVisionSlider;
}


[System.Serializable]
//这是一个结构体，用于表示库存UI的标签组项
public struct InventoryUI_TabGroupItem
{
    public Text TabText;
    public GameObject LayoutContainer;
}

[System.Serializable]
public struct InventoryUI_TabGroup
{
    public List<InventoryUI_TabGroupItem> Items;
}

[System.Serializable]
public struct InventoryUI_DescriptionLayout
{
    public GameObject LayoutContainer;
    public Image Image;
    public Text Title;
    public ScrollRect ScrollView;
    public Text Description;
}

[System.Serializable]
public struct InventoryUI_ActionButton
{
    public GameObject GameObject;
    public Text ButtonText;
}

public class PlayerInventoryUI : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField]
    protected Inventory _inventory = null;
    [SerializeField] protected FPSController _fpsController = null;

    [Header("Equipment Mount References")]
    [SerializeField]
    //这是一个列表，用于存储背包挂载点的游戏对象
    protected List<GameObject> _backpackMounts = new List<GameObject>();
    protected List<Image> _backpackMountImages = new List<Image>();
    protected List<Text> _backpackMountText = new List<Text>();

    // 武器挂载点
    [SerializeField]
    protected List<GameObject> _weaponMounts = new List<GameObject>();
    protected List<Text> _weaponMountNames = new List<Text>();
    protected List<Slider> _weaponMountSliders = new List<Slider>();
    protected List<Image> _weaponMountImages = new List<Image>();
    protected List<GameObject> _weaponMountAmmoInfo = new List<GameObject>();
    protected List<Text> _weaponMountRounds = new List<Text>();
    protected List<Text> _weaponMountReloadType = new List<Text>();


    [SerializeField]
    protected List<GameObject> _ammoMounts = new List<GameObject>();
    protected List<Image> _ammoMountImages = new List<Image>();
    protected List<Text> _ammoMountEmptyText = new List<Text>();
    protected List<Text> _ammoMountRoundsText = new List<Text>();
    // PDA界面引用
    [SerializeField] protected InventoryUI_PDAReferences _pDAReferences;

    // 检视面板中指定的仪表引用
    [Header("UI Meter References")]
    [SerializeField] protected InventoryUI_Status _statusPanelUI;

    [Header("Backpack / PDA Tab Group")]
    [SerializeField] protected InventoryUI_TabGroup _tabGroup;

    [Header("Description Layouts")]
    [SerializeField] protected InventoryUI_DescriptionLayout _generalDescriptionLayout;
    [SerializeField] protected InventoryUI_DescriptionLayout _weaponDescriptionLayout;

    // 操作按钮界面引用
    [Header("Action Button UI References")]
    [SerializeField] protected InventoryUI_ActionButton _actionButton1;
    [SerializeField] protected InventoryUI_ActionButton _actionButton2;

    [Header("Shared Variables")]
    [SerializeField] SharedFloat _health = null;
    [SerializeField] SharedFloat _infection = null;
    [SerializeField] SharedFloat _stamina = null;
    [SerializeField] SharedFloat _flashlight = null;
    [SerializeField] SharedFloat _nightvision = null;

    [Header("Colors")]
    [SerializeField] Color _tabTextHover = Color.cyan;
    [SerializeField] Color _tabTextInactive = Color.cyan;
    [SerializeField] Color _backpackMountHover = Color.cyan;
    [SerializeField] Color _ammoMountHover = Color.cyan;
    [SerializeField] Color _weaponMountHover = Color.cyan;

    public Inventory inventory { get { return _inventory; } set { _inventory = value; } }

    // 内部变量
    protected Color _backpackMountColor;
    protected Color _weaponMountColor;
    protected Color _ammoMountColor;
    protected Color _tabTextColor;

    protected InventoryPanelType _selectedPanelType = InventoryPanelType.None;
    protected int _selectedMount = -1;
    protected bool _isInitialized = false;
    protected int _activeTab = 0;
    protected bool _audioPlayOnPickup = true;

    protected virtual void OnEnable()
    {
        if (_fpsController == null) _fpsController = FindObjectOfType<FPSController>();
        if (_fpsController != null)
            _fpsController.SetUIInputMode(true);
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 更新界面，以重置后的状态显示当前库存
        Invalidate();
    }

    protected virtual void OnDisable()
    {
        if (_fpsController != null)
            _fpsController.SetUIInputMode(false);
    }

    protected virtual void Invalidate()
    {
        // 确保在首次渲染前完成初始化
        if (!_isInitialized)
            Initialize();

        // 重置选择状态
        _selectedPanelType = InventoryPanelType.None;
        _selectedMount = -1;

        // 禁用说明面板
        if (_generalDescriptionLayout.LayoutContainer != null)
            _generalDescriptionLayout.LayoutContainer.SetActive(false);

        if (_weaponDescriptionLayout.LayoutContainer != null)
            _weaponDescriptionLayout.LayoutContainer.SetActive(false);

        // 禁用操作按钮
        if (_actionButton1.GameObject != null)
            _actionButton1.GameObject.SetActive(false);

        if (_actionButton2.GameObject != null)
            _actionButton2.GameObject.SetActive(false);

        // 清空武器挂载点
        for (int i = 0; i < _weaponMounts.Count; i++)
        {
            if (_weaponMounts[i] != null)
            {
                if (_weaponMountImages[i] != null) _weaponMountImages[i].sprite = null;
                if (_weaponMountNames[i] != null) _weaponMountNames[i].text = "";
                if (_weaponMountSliders[i] != null) _weaponMountSliders[i].enabled = false;

                _weaponMounts[i].SetActive(false);
                _weaponMounts[i].transform.GetComponent<Image>().fillCenter = false;
            }
        }

        // 遍历界面中的背包挂载点，将它们全部设为空且未选中
        for (int i = 0; i < _backpackMounts.Count; i++)
        {
            // 清除精灵并禁用挂载点
            if (_backpackMountImages[i] != null)
            {
                _backpackMountImages[i].gameObject.SetActive(false);
                _backpackMountImages[i].sprite = null;
            }

            // 启用该槽位中显示“EMPTY”的文字
            if (_backpackMountText[i] != null)
                _backpackMountText[i].gameObject.SetActive(true);

            // 让所有挂载点呈现未选中状态
            if (_backpackMounts[i] != null)
            {
                // 获取挂载点自身的Image组件（边框）
                Image img = _backpackMounts[i].GetComponent<Image>();
                if (img)
                {
                    img.fillCenter = false;
                    img.color = _backpackMountColor;
                }
            }
        }

        // 配置弹药槽
        for (int i = 0; i < _ammoMounts.Count; i++)
        {
            // 清除精灵并禁用挂载点
            if (_ammoMounts[i] != null)
            {
                if (_ammoMountImages[i])
                {
                    _ammoMountImages[i].gameObject.SetActive(false);
                    _ammoMountImages[i].sprite = null;
                }
            }

            // 启用该槽位中显示“EMPTY”的文字
            if (_ammoMountEmptyText[i] != null) _ammoMountEmptyText[i].gameObject.SetActive(true);
            if (_ammoMountRoundsText[i] != null) _ammoMountRoundsText[i].gameObject.SetActive(false);

            // 让挂载点边框呈现未选中状态
            if (_ammoMounts[i] != null)
            {
                Image img = _ammoMounts[i].GetComponent<Image>();
                if (img)
                {
                    img.fillCenter = false;
                    img.color = _ammoMountColor;
                }
            }
        }

        // 其他PDA相关设置
        if (_pDAReferences._autoplayOnPickup)
        {
            if (_audioPlayOnPickup)
                _pDAReferences._autoplayOnPickup.isOn = true;
            else
                _pDAReferences._autoplayOnPickup.isOn = false;
        }

        // 最后更新状态面板
        if (_statusPanelUI.HealthSlider) _statusPanelUI.HealthSlider.value = _health.value;
        if (_statusPanelUI.InfectionSlider) _statusPanelUI.InfectionSlider.value = _infection.value;
        if (_statusPanelUI.StaminaSlider) _statusPanelUI.StaminaSlider.value = _stamina.value;
        if (_statusPanelUI.FlashlightSlider) _statusPanelUI.FlashlightSlider.value = _flashlight.value;
        if (_statusPanelUI.NightVisionSlider) _statusPanelUI.NightVisionSlider.value = _nightvision.value;

        if (_inventory != null)
        {
            // Configure Weapons Panel by iterating through each mount
            for (int i = 0; i < _weaponMounts.Count; i++)
            {
                // Do we have a weapon mount here
                if (_weaponMounts[i] != null)
                {
                    // Get the matching mount and weapon data from the inventory
                    InventoryWeaponMountInfo weaponMountInfo = _inventory.GetWeapon(i);
                    InventoryItemWeapon weapon = null;
                    if (weaponMountInfo != null)
                        weapon = weaponMountInfo.Weapon;

                    // No weapon info here to skip this mount
                    if (weapon == null) continue;

                    // Set sprite and name of weapon
                    if (_weaponMountImages[i] != null) _weaponMountImages[i].sprite = weapon.inventoryImage;
                    if (_weaponMountNames[i] != null) _weaponMountNames[i].text = weapon.inventoryName;

                    // If its a melee weapon then deactivate the entire AmmoInfo section of the UI
                    // otherwise Enabled it and show the Reload Type and Rounds in Gun
                    if (_weaponMountAmmoInfo[i] != null)
                    {
                        if (weapon.weaponFeedType == InventoryWeaponFeedType.Melee)
                        {
                            _weaponMountAmmoInfo[i].SetActive(false);
                        }
                        else
                        {
                            // Activate Mount
                            _weaponMountAmmoInfo[i].SetActive(true);

                            // Display Reload Type
                            if (_weaponMountReloadType[i] != null)
                                _weaponMountReloadType[i].text = weapon.reloadType.ToString();


                            if (_weaponMountRounds[i] != null)
                                _weaponMountRounds[i].text = weaponMountInfo.InGunRounds + " / " + weapon.ammoCapacity;
                        }
                    }

                    // Update the condition slider
                    if (_weaponMountSliders[i] != null)
                    {
                        _weaponMountSliders[i].enabled = true;
                        _weaponMountSliders[i].value = weaponMountInfo.Condition;
                    }

                    _weaponMounts[i].SetActive(true);
                }
            }

            // Configure Ammo Mounts
            for (int i = 0; i < _ammoMounts.Count; i++)
            {
                // Clear Sprite and deactivate mount
                if (_ammoMounts[i] != null)
                {
                    // Get the ammo and it's mount info for this mount
                    InventoryAmmoMountInfo ammoMountInfo = _inventory.GetAmmo(i);
                    InventoryItemAmmo ammo = null;
                    if (ammoMountInfo != null)
                        ammo = ammoMountInfo.Ammo;

                    // No weapon at this mount so skip
                    if (ammo == null) continue;

                    // Set image
                    if (_ammoMountImages[i])
                    {
                        _ammoMountImages[i].gameObject.SetActive(true);
                        _ammoMountImages[i].sprite = ammoMountInfo.Ammo.inventoryImage;
                    }
                    // Set and Enable Rounds Text
                    if (_ammoMountRoundsText[i] != null)
                    {
                        _ammoMountRoundsText[i].gameObject.SetActive(true);
                        _ammoMountRoundsText[i].text = ammoMountInfo.Rounds.ToString();
                    }

                    // Disable Empty text
                    if (_ammoMountEmptyText[i] != null)
                        _ammoMountEmptyText[i].gameObject.SetActive(false);
                }

            }

            // Iterate over the UI Backpack mounts and set all to empty and unselected
            for (int i = 0; i < _backpackMounts.Count; i++)
            {
                if (_backpackMounts[i] != null)
                {
                    InventoryBackpackMountInfo backpackMountInfo = _inventory.GetBackpack(i);
                    InventoryItem item = null;
                    if (backpackMountInfo != null)
                        item = backpackMountInfo.Item;

                    if (item != null)
                    {
                        // Set sprite and activate mount
                        if (_backpackMountImages[i] != null)
                        {
                            _backpackMountImages[i].gameObject.SetActive(true);
                            _backpackMountImages[i].sprite = item.inventoryImage;
                        }

                        // Disable the text for this slot that says "EMPTY"
                        if (_backpackMountText[i] != null)
                            _backpackMountText[i].gameObject.SetActive(false);
                    }
                }
            }
        }

    }


    // --------------------------------------------------------------------------------------------
    // 名称：Initialize
    // 说明：此函数只会在InventoryUI游戏对象首次启用并显示库存时调用一次。
    //       它会在界面层级中查找其他界面对象并缓存对它们的引用，
    //       从而省去在检视面板中逐一绑定所有引用的工作。
    // --------------------------------------------------------------------------------------------
    protected virtual void Initialize()
    {
        // 此函数只能调用一次
        if (_isInitialized) return;

        // 标记此函数已经调用
        _isInitialized = true;

        // 缓存背包边框的初始颜色，以便在未选中时恢复
        if (_backpackMounts.Count > 0 && _backpackMounts[0] != null)
        {
            Image tmp = _backpackMounts[0].GetComponent<Image>();
            if (tmp) _backpackMountColor = tmp.color;
        }

        // 对弹药挂载点边框执行相同操作
        if (_ammoMounts.Count > 0 && _ammoMounts[0] != null)
        {
            Image tmp = _ammoMounts[0].GetComponent<Image>();
            if (tmp) _ammoMountColor = tmp.color;
        }

        // 对武器挂载点边框执行相同操作
        if (_weaponMounts.Count > 0 && _weaponMounts[0] != null)
        {
            Image tmp = _weaponMounts[0].GetComponent<Image>();
            if (tmp) _weaponMountColor = tmp.color;
        }

        // 三种槽位使用弹药栏相同的悬浮色和选中底色。
        _backpackMountHover = _ammoMountHover;
        _weaponMountHover = _ammoMountHover;
        _backpackMountColor = _ammoMountColor;
        _weaponMountColor = _ammoMountColor;

        // 记录标签文字的正常颜色
        if (_tabGroup.Items.Count > 0 &&
            _tabGroup.Items[0].TabText != null)
        {
            _tabTextColor = _tabGroup.Items[0].TabText.color;
        }

        // 缓存每个背包挂载点的Image和Text界面引用
        for (int i = 0; i < _backpackMounts.Count; i++)
        {
            // 初始时所有槽位都为空
            _backpackMountImages.Add(null);
            _backpackMountText.Add(null);

            // 缓存槽位的Image和Text引用
            // 每个槽位应当恰好包含一个Image游戏对象和一个Text游戏对象
            if (_backpackMounts[i] != null)
            {
                Transform parent = _backpackMounts[i].transform;
                Transform tmp;
                tmp = parent.Find("Image");
                if (tmp) _backpackMountImages[i] = tmp.GetComponent<Image>();

                tmp = parent.Find("Text");
                if (tmp) _backpackMountText[i] = tmp.GetComponent<Text>();

            }
        }

        // 缓存每个弹药挂载点的界面子对象引用
        for (int i = 0; i < _ammoMounts.Count; i++)
        {
            _ammoMountImages.Add(null);
            _ammoMountEmptyText.Add(null);
            _ammoMountRoundsText.Add(null);

            if (_ammoMounts[i] != null)
            {
                Transform parent = _ammoMounts[i].transform;
                Transform tmp;
                tmp = parent.Find("Image");
                if (tmp) _ammoMountImages[i] = tmp.GetComponent<Image>();
                tmp = parent.Find("Empty");
                if (tmp) _ammoMountEmptyText[i] = tmp.GetComponent<Text>();
                tmp = parent.Find("Rounds");
                if (tmp) _ammoMountRoundsText[i] = tmp.GetComponent<Text>();
            }
        }

        // 缓存所有武器挂载点引用
        for (int i = 0; i < _weaponMounts.Count; i++)
        {
            // 在列表中创建条目
            _weaponMountNames.Add(null);
            _weaponMountImages.Add(null);
            _weaponMountAmmoInfo.Add(null);
            _weaponMountReloadType.Add(null);
            _weaponMountRounds.Add(null);
            _weaponMountSliders.Add(null);

            // 接下来绑定武器挂载点的所有引用
            if (_weaponMounts[i] != null)
            {
                Transform trans = _weaponMounts[i].transform;
                Transform tmp;

                // 查找子对象
                tmp = trans.Find("Image");
                if (tmp) { _weaponMountImages[i] = tmp.GetComponent<Image>(); }
                tmp = trans.Find("Name");
                if (tmp) { _weaponMountNames[i] = tmp.GetComponent<Text>(); }
                tmp = trans.Find("Slider");
                if (tmp) { _weaponMountSliders[i] = tmp.GetComponent<Slider>(); }
                tmp = trans.Find("Ammo Info");
                if (tmp) _weaponMountAmmoInfo[i] = tmp.gameObject;
                tmp = trans.Find("Ammo Info/Rounds");
                if (tmp) _weaponMountRounds[i] = tmp.GetComponent<Text>();
                tmp = trans.Find("Ammo Info/Reload Type");
                if (tmp) _weaponMountReloadType[i] = tmp.GetComponent<Text>();
            }
        }

        // 在标签组中只选择一个互斥面板
        SelectTabGroup(_activeTab);
    }

    public void SelectTabGroup(int panel)
    {
        _activeTab = panel;

        // 遍历标签组中的所有标签
        for (int i = 0; i < _tabGroup.Items.Count; i++)
        {
            // 如果这是选中的标签，则启用其布局，
            // 并将标签文字设置为激活颜色
            if (i == _activeTab)
            {
                if (_tabGroup.Items[i].LayoutContainer)
                    _tabGroup.Items[i].LayoutContainer.SetActive(true);

                if (_tabGroup.Items[i].TabText)
                    _tabGroup.Items[i].TabText.color = _tabTextColor;
            }
            else
            {
                // 这不是激活的标签，因此禁用布局，
                // 并将标签文字设置为未激活颜色
                if (_tabGroup.Items[i].LayoutContainer)
                    _tabGroup.Items[i].LayoutContainer.SetActive(false);

                if (_tabGroup.Items[i].TabText)
                    _tabGroup.Items[i].TabText.color = _tabTextInactive;
            }
        }
    }

    protected void DisplayWeaponDescription(InventoryItem item)
    {
        if (item == null)
        {
            HideDescription();
            return;
        }

        // 禁用非武器布局
        if (_generalDescriptionLayout.LayoutContainer != null)
            _generalDescriptionLayout.LayoutContainer.SetActive(false);

        // 启用武器布局
        if (_weaponDescriptionLayout.LayoutContainer != null)
            _weaponDescriptionLayout.LayoutContainer.SetActive(true);

        if (_weaponDescriptionLayout.Image != null) _weaponDescriptionLayout.Image.sprite = item.inventoryImage;
        if (_weaponDescriptionLayout.Title != null) _weaponDescriptionLayout.Title.text = item.inventoryName;
        if (_weaponDescriptionLayout.Description != null) _weaponDescriptionLayout.Description.text = item.inventoryDescription;

        // 启用操作按钮
        if (_actionButton1.GameObject != null)
        {
            _actionButton1.GameObject.SetActive(item.inventoryAction != InventoryAction.None);
            if (_actionButton1.ButtonText != null) _actionButton1.ButtonText.text = item.inventoryActionText;
        }

        if (_actionButton2.GameObject != null)
        {
            _actionButton2.GameObject.SetActive(true);
            if (_actionButton2.ButtonText != null) _actionButton2.ButtonText.text = "Drop";
        }

        if (_weaponDescriptionLayout.ScrollView != null) _weaponDescriptionLayout.ScrollView.verticalNormalizedPosition = 1.0f;
    }

    protected void DisplayGeneralDescription(InventoryItem item)
    {
        if (item == null)
        {
            HideDescription();
            return;
        }

        // 启用非武器布局
        if (_generalDescriptionLayout.LayoutContainer != null)
            _generalDescriptionLayout.LayoutContainer.SetActive(true);

        // 禁用武器布局
        if (_weaponDescriptionLayout.LayoutContainer != null)
            _weaponDescriptionLayout.LayoutContainer.SetActive(false);

        if (_generalDescriptionLayout.Image != null) _generalDescriptionLayout.Image.sprite = item.inventoryImage;
        if (_generalDescriptionLayout.Title != null) _generalDescriptionLayout.Title.text = item.inventoryName;
        if (_generalDescriptionLayout.Description != null) _generalDescriptionLayout.Description.text = item.inventoryDescription;

        // 启用操作按钮
        if (_actionButton1.GameObject != null)
        {
            _actionButton1.GameObject.SetActive(item.inventoryAction != InventoryAction.None);
            if (_actionButton1.ButtonText != null) _actionButton1.ButtonText.text = item.inventoryActionText;
        }

        if (_actionButton2.GameObject != null)
        {
            _actionButton2.GameObject.SetActive(true);
            if (_actionButton2.ButtonText != null) _actionButton2.ButtonText.text = "Drop";
        }

        if (_generalDescriptionLayout.ScrollView != null) _generalDescriptionLayout.ScrollView.verticalNormalizedPosition = 1.0f;
    }

    protected void HideDescription()
    {
        // 禁用非武器布局
        if (_generalDescriptionLayout.LayoutContainer != null)
            _generalDescriptionLayout.LayoutContainer.SetActive(false);

        // 禁用武器布局
        if (_weaponDescriptionLayout.LayoutContainer != null)
            _weaponDescriptionLayout.LayoutContainer.SetActive(false);

        // 禁用操作按钮
        if (_actionButton1.GameObject != null)
            _actionButton1.GameObject.SetActive(false);

        if (_actionButton2.GameObject != null)
            _actionButton2.GameObject.SetActive(false);
    }
    public void OnEnterBackpackMount(Image image)
    {
        int mount;

        // 从传入对象的名称中取得槽位索引
        if (image == null || !int.TryParse(image.name, out mount))
        {
            Debug.Log("OnEnterBackpackMount Error! Could not parse image name as INT");
            return;
        }

        // 索引是否有效？
        if (mount >= 0 && mount < _backpackMounts.Count)
        {
            InventoryBackpackMountInfo itemMount = _inventory != null ? _inventory.GetBackpack(mount) : null;
            if (itemMount == null || itemMount.Item == null) return;

            // 将该槽位的边框颜色设为悬停颜色
            if (_selectedPanelType != InventoryPanelType.Backpack || _selectedMount != mount)
                image.color = _backpackMountHover;

            // 如果选中的面板不是None，说明当前已选中其他对象，
            // 因此不要更新信息面板
            if (_selectedPanelType != InventoryPanelType.None) return;

            // 更新说明窗口
            DisplayGeneralDescription(itemMount.Item);
        }

    }

    public void OnExitBackpackMount(Image image)
    {

        if (image != null)
            image.color = _backpackMountColor;

        if (_selectedPanelType != InventoryPanelType.None) return;

        // 隐藏说明窗口
        HideDescription();
    }

    public void OnClickBackpackMount(Image image)
    {
        // 从名称中取得挂载点索引
        int mount;
        if (image == null || !int.TryParse(image.name, out mount))
        {
            Debug.Log("OnClickBackpackError : Could not parse image name as INT");
            return;
        }

        // 检查被点击的挂载点是否有效
        if (mount >= 0 && mount < _backpackMounts.Count)
        {
            InventoryBackpackMountInfo itemMount = _inventory != null ? _inventory.GetBackpack(mount) : null;
            if (itemMount == null || itemMount.Item == null) return;

            // 点击的是当前已选中的物品，因此取消选择
            if (mount == _selectedMount && _selectedPanelType == InventoryPanelType.Backpack)
            {
                Invalidate();
                image.color = _backpackMountHover;
                image.fillCenter = false;
                DisplayGeneralDescription(itemMount.Item);
            }
            else
            {
                Invalidate();
                _selectedPanelType = InventoryPanelType.Backpack;
                _selectedMount = mount;
                image.color = _backpackMountColor;
                image.fillCenter = true;
                DisplayGeneralDescription(itemMount.Item);
            }

        }
    }

    public void OnEnterAmmoMount(Image image)
    {
        int mount;
        if (image == null || !int.TryParse(image.name, out mount))
        {
            Debug.Log("OnEnterAmmoMount Error! Could not parse image name as int");
            return;
        }

        // 槽位索引是否有效？
        if (mount >= 0 && mount < _ammoMounts.Count)
        {
            InventoryAmmoMountInfo itemMount = _inventory != null ? _inventory.GetAmmo(mount) : null;
            if (itemMount == null || itemMount.Ammo == null) return;

            // 设置边框的悬停颜色
            if (_selectedPanelType != InventoryPanelType.AmmoBelt || _selectedMount != mount)
                image.color = _ammoMountHover;

            // 如果已有对象被选中，则直接返回且不更新信息面板
            if (_selectedPanelType != InventoryPanelType.None) return;

            // 更新说明窗口
            DisplayGeneralDescription(itemMount.Ammo);
        }
    }

    public void OnClickAmmoMount(Image image)
    {
        int mount;
        if (image == null || !int.TryParse(image.name, out mount))
        {
            Debug.Log("OnEnterAmmoMount Error! Could not parse image name as int");
            return;
        }

        // 检查被点击的挂载点是否有效
        if (mount >= 0 && mount < _ammoMounts.Count)
        {
            InventoryAmmoMountInfo itemMount = _inventory != null ? _inventory.GetAmmo(mount) : null;
            if (itemMount == null || itemMount.Ammo == null) return;

            // 点击的是当前已选中的物品，因此取消选择
            if (mount == _selectedMount && _selectedPanelType == InventoryPanelType.AmmoBelt)
            {
                Invalidate();
                image.color = _ammoMountHover;
                image.fillCenter = false;
                DisplayGeneralDescription(itemMount.Ammo);
            }
            else
            {
                Invalidate();
                _selectedPanelType = InventoryPanelType.AmmoBelt;
                _selectedMount = mount;
                image.color = _ammoMountColor;
                image.fillCenter = true;
                DisplayGeneralDescription(itemMount.Ammo);
            }
        }
    }

    public void OnExitAmmoMount(Image image)
    {

        // 将边框Image恢复为初始颜色（未选中／未悬停）
        if (image != null)
        {
            image.color = _ammoMountColor;
            if (_selectedPanelType != InventoryPanelType.None) return;
        }

        // 隐藏说明面板
        HideDescription();
    }

    public void OnEnterWeaponMount(Image image)
    {
        int mount;
        if (image == null || !int.TryParse(image.name, out mount))
        {
            Debug.Log("OnEnterWeaponMount Error! Could not parse image name as int");
            return;
        }

        // 挂载点索引是否有效？
        if (mount >= 0 && mount < _weaponMounts.Count)
        {
            InventoryWeaponMountInfo itemMount = _inventory != null ? _inventory.GetWeapon(mount) : null;
            if (itemMount == null || itemMount.Weapon == null) return;

            // 设置边框的悬停颜色
            if (_selectedPanelType != InventoryPanelType.Weapons || _selectedMount != mount)
                image.color = _weaponMountHover;

            // 如果已有对象被选中，则直接返回且不更新信息面板
            if (_selectedPanelType != InventoryPanelType.None) return;

            // 显示说明窗口
            DisplayWeaponDescription(itemMount.Weapon);
        }
    }

    public void OnClickWeaponMount(Image image)
    {
        int mount;
        if (image == null || !int.TryParse(image.name, out mount))
        {
            Debug.Log("OnEnterWeaponMount Error! Could not parse image name as int");
            return;
        }

        // 检查被点击的挂载点是否有效
        if (mount >= 0 && mount < _weaponMounts.Count)
        {
            InventoryWeaponMountInfo itemMount = _inventory != null ? _inventory.GetWeapon(mount) : null;
            if (itemMount == null || itemMount.Weapon == null) return;

            // 点击的是当前已选中的物品，因此取消选择
            if (mount == _selectedMount && _selectedPanelType == InventoryPanelType.Weapons)
            {
                Invalidate();
                image.color = _weaponMountHover;
                image.fillCenter = false;
                DisplayWeaponDescription(itemMount.Weapon);
            }
            else
            {
                Invalidate();
                _selectedPanelType = InventoryPanelType.Weapons;
                _selectedMount = mount;
                image.color = _weaponMountColor;
                image.fillCenter = true;
                DisplayWeaponDescription(itemMount.Weapon);
            }
        }
    }

    public void OnExitWeaponMount(Image image)
    {

        // 将边框Image恢复为初始颜色（未选中／未悬停）
        if (image != null)
        {
            image.color = _weaponMountColor;
            if (_selectedPanelType != InventoryPanelType.None) return;
        }

        // 隐藏说明窗口
        HideDescription();
    }


    public void OnEnterTab(int index)
    {
        if (index >= 0 && index < _tabGroup.Items.Count)
        {
            if (_tabGroup.Items[index].TabText != null)
            {
                _tabGroup.Items[index].TabText.color = (_activeTab != index) ? _tabTextHover : _tabTextColor;
            }
        }
    }

    public void OnExitTab(int index)
    {
        if (index >= 0 && index < _tabGroup.Items.Count)
        {
            if (_tabGroup.Items[index].TabText != null)
            {
                _tabGroup.Items[index].TabText.color = (_activeTab == index) ? _tabTextColor : _tabTextInactive;
            }
        }
    }

    public void OnClickTab(int index)
    {
        if (index >= 0 && index < _tabGroup.Items.Count)
        {
            SelectTabGroup(index);
        }
    }

    public void OnActionButton1()
    {
        if (_inventory == null) return;

        switch (_selectedPanelType)
        {
            case InventoryPanelType.Backpack: _inventory.UseBackpackItem(_selectedMount); break;
            case InventoryPanelType.Weapons: _inventory.ReloadWeapon(_selectedMount); break;
        }

        Invalidate();
    }

    public void OnActionButton2()
    {
        if (_inventory == null) return;

        switch (_selectedPanelType)
        {
            case InventoryPanelType.Backpack: _inventory.DropBackpackItem(_selectedMount); break;
            case InventoryPanelType.Weapons: _inventory.DropWeaponItem(_selectedMount); break;
            case InventoryPanelType.AmmoBelt: _inventory.DropAmmoItem(_selectedMount); break;
        }

        Invalidate();
    }

}
