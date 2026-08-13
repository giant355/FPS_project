using UnityEngine;

public class InteractiveInfo : InteractiveItem
{
    //3：文本框至少显示 3 行高度
    //10：文本框最多自动扩展到 10 行高度
    //超过 10 行后，并不是不能继续输入，而是文本框不再继续变高，会通过滚动来查看内容
    [TextArea(3, 10)]
    [SerializeField] private string _infoText = null;

    // 返回显示在 HUD 上的物品说明
    public override string GetText()
    {
        return _infoText;
    }
}