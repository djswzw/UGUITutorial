using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("UI/Extensions/Loop ScrollRect/Horizontal Loop ScrollRect")]
public class HorizontalLoopScrollRect : LoopScrollRect
{
    private int m_LastStartIndex = -1;
    private int m_LastEndIndex = -1;
    private float m_ContentWidth = 0;

    protected override void Awake()
    {
        base.Awake();
        // 关键：确保Content的Pivot和Anchor设置正确，以匹配水平滚动
        // Pivot X = 0 (左侧)
        // Anchor Min X = 0, Max X = 0 (左侧对齐)
        // Anchor Min/Max Y = 0/1 (垂直拉伸)
        rectTransform.pivot = new Vector2(0, 0.5f);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 1);
    }

    protected override void OnRebuild()
    {
        base.OnRebuild(); 
        RecycleAllActiveItems();
        m_LastStartIndex = -1;
        m_LastEndIndex = -1;
    }

    /// <summary>
    /// UGUI布局系统回调：计算Content的首选宽度。
    /// </summary>
    public override void CalculateLayoutInputHorizontal()
    {
        if (m_ItemSize == Vector2.zero && itemPrefab != null)
        {
            m_ItemSize = itemPrefab.GetComponent<RectTransform>().sizeDelta;
        }
        m_ContentWidth = (m_ItemSize.x * m_TotalCount) + (m_Spacing * (m_TotalCount > 0 ? (m_TotalCount - 1) : 0)) + padding.horizontal;
        SetLayoutInputForAxis(m_ContentWidth, m_ContentWidth, 0, 0); 
    }

    public override void SetLayoutVertical() { }


    public override void SetLayoutHorizontal()
    {
        float viewWidth = scrollRect.viewport.rect.width;
        float finalContentWidth = Mathf.Max(m_ContentWidth, viewWidth);

        if (!Mathf.Approximately(rectTransform.sizeDelta.x, finalContentWidth))
        {
            rectTransform.sizeDelta = new Vector2(finalContentWidth, rectTransform.sizeDelta.y);
        }
        UpdateVisibleItems();
    }

    private void UpdateVisibleItems()
    {
        if (m_ItemSize.x <= 0 || m_TotalCount == 0)
        {
            RecycleAllActiveItems();
            return;
        }

        float startXOffset = GetStartOffset(0, m_ContentWidth - padding.horizontal);
        float viewWidth = scrollRect.viewport.rect.width;
        // contentPos 是Content的左侧，相对于Viewport左侧的偏移量 (向右滚动为负值)
        float contentPos = rectTransform.anchoredPosition.x;
        float viewLeftPosInContent = -contentPos - startXOffset;

        int newStartIndex = Mathf.FloorToInt(viewLeftPosInContent / (m_ItemSize.x + m_Spacing));
        newStartIndex = Mathf.Max(0, newStartIndex);

        float viewRightPosInContent = viewLeftPosInContent + viewWidth;
        int newEndIndex = Mathf.CeilToInt(viewRightPosInContent / (m_ItemSize.x + m_Spacing));
        newEndIndex = Mathf.Min(m_TotalCount - 1, newEndIndex);

        if (newStartIndex == m_LastStartIndex && newEndIndex == m_LastEndIndex)
        {
            return;
        }

        // 增量式回收 (只回收完全离开视野的) ---
        var keysToRemove = new List<int>();
        foreach (var pair in m_ActiveItems)
        {
            int index = pair.Key;
            if (index < newStartIndex || index > newEndIndex)
            {
                m_ItemsToRecycle.Add(pair.Value);
                keysToRemove.Add(index);
            }
        }
        foreach (var key in keysToRemove)
        {
            m_ActiveItems.Remove(key);
        }

        //增量式创建 (只创建新进入视野的) ---
        for (int i = newStartIndex; i <= newEndIndex; i++)
        {
            if (m_ActiveItems.ContainsKey(i))
            {
                continue;
            }

            RectTransform item = GetOrCreateItem();
            m_ActiveItems.Add(i, item);

            PositionItem(item, i, startXOffset);

            UpdateItem(item.gameObject, i);
        }

        //更新上一帧的索引记录，并启动回收
        m_LastStartIndex = newStartIndex;
        m_LastEndIndex = newEndIndex;
        RecycleItems();
    }

    private void PositionItem(RectTransform item, int dataIndex, float startXOffset)
    {
        item.anchorMin = new Vector2(0, 1);
        item.anchorMax = new Vector2(0, 1);

        // --- 水平位置 (X) ---
        float xPos = startXOffset + (dataIndex * (m_ItemSize.x + m_Spacing));

        // --- 垂直位置 (Y)，支持对齐 ---
        float contentHeight = rectTransform.rect.height - padding.vertical;
        float itemHeight = item.sizeDelta.y;
        float verticalAlignment = GetAlignmentOnAxis(1); // 0 for Top, 0.5 for Middle, 1 for Bottom
        float yPos = -(contentHeight - itemHeight) * verticalAlignment;

        item.anchoredPosition = new Vector2(xPos, yPos);
    }

    private RectTransform GetOrCreateItem()
    {
        RectTransform item;
        if (m_ItemPool.Count > 0)
        {
            item = m_ItemPool.Dequeue();
        }
        else
        {
            item = Instantiate(itemPrefab, transform).GetComponent<RectTransform>();
        }
        item.gameObject.SetActive(true);
        return item;
    }

    private void RecycleAllActiveItems()
    {
        if (m_ActiveItems.Count == 0) return;
        foreach (var pair in m_ActiveItems) m_ItemsToRecycle.Add(pair.Value);
        m_ActiveItems.Clear();
        RecycleItems();
    }

    protected override int GetFirstVisibleItemIndex()
    {
        if (m_ItemSize.x <= 0) return 0;

        float startXOffset = GetStartOffset(0, m_ContentWidth - padding.horizontal);
        float contentPos = rectTransform.anchoredPosition.x;
        float viewLeftPosInContent = -contentPos - startXOffset;

        int index = Mathf.FloorToInt(viewLeftPosInContent / (m_ItemSize.x + m_Spacing));
        return Mathf.Max(0, index);
    }

    protected override Vector2 GetPositionForIndex(int index)
    {
        // X 坐标 = - (左侧内边距 + 锚点索引之前的总宽度)
        // 注意是负值，因为向右滚动时 anchoredPosition.x 为负
        float xPos = -(padding.left + index * (m_ItemSize.x + m_Spacing));
        return new Vector2(xPos, rectTransform.anchoredPosition.y);
    }

    protected override float NormalizedPosition
    {
        get { return scrollRect.horizontalNormalizedPosition; }
        set { scrollRect.horizontalNormalizedPosition = value; }
    }

    public override void ScrollTo(int index, bool immediate = false)
    {
        if (m_TotalCount <= 0) return;
        index = Mathf.Clamp(index, 0, m_TotalCount - 1);

        float itemPosX = (padding.left + index * (m_ItemSize.x + m_Spacing));
        float totalWidth = m_ContentWidth;
        float viewWidth = scrollRect.viewport.rect.width;

        float scrollableWidth = totalWidth - viewWidth;
        if (scrollableWidth <= 0)
        {
            DoScrollTo(0f, immediate);
            return;
        }

        // 0.0f 对应左侧, 1.0f 对应右侧
        float targetNormalizedPos = itemPosX / scrollableWidth;
        targetNormalizedPos = Mathf.Clamp01(targetNormalizedPos);

        DoScrollTo(targetNormalizedPos, immediate);
    }
}