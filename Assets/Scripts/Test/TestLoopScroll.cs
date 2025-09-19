using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestLoopScroll : MonoBehaviour
{
    public VerticalLoopScrollRect loopScrollRect;
    public HorizontalLoopScrollRect hloopScrollRect;
    public List<string> myData; // 我们的数据源

    void Start()
    {
        // 准备一些测试数据
        myData = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            myData.Add("Item " + i);
        }

        // 订阅Item更新事件
        loopScrollRect.OnItemUpdate += OnItemUpdate;

        // 向循环列表提供数据
        loopScrollRect.ProvideData(myData.Count);

        // 订阅Item更新事件
        hloopScrollRect.OnItemUpdate += OnItemUpdate;

        // 向循环列表提供数据
        hloopScrollRect.ProvideData(myData.Count);
    }

    // 当Item需要更新显示时，此方法会被调用
    void OnItemUpdate(GameObject item, int dataIndex)
    {
        // 从Item中找到Text组件并更新其内容
        var textComponent = item.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = myData[dataIndex];
        }

        // 你还可以在这里更新Image或其他组件
        item.name = "Item_" + dataIndex;
    }

    public void Reset()
    {
        myData = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            myData.Add("Item " + i);
        }
        loopScrollRect.ProvideData(myData.Count);
        hloopScrollRect.ProvideData(myData.Count);
    }

    public void Jump()
    {
        loopScrollRect.ScrollTo(500, false);
    }

    public void JumpHorizon()
    {
        hloopScrollRect.ScrollTo(500, false);
    }
}