using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("UI/Extensions/Loop ScrollRect/Vertical Loop ScrollRect")]
public class VerticalLoopScrollRect : LoopScrollRect
{
    private int m_LastStartIndex = -1;
    private int m_LastEndIndex = -1;
    private float m_ContentHeight = 0;

    protected override void Awake()
    {
        base.Awake();
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
    }
    protected override void OnRebuild()
    {
        base.OnRebuild();
        RecycleAllActiveItems();
        m_LastStartIndex = -1;
        m_LastEndIndex = -1;
    }

    public override void CalculateLayoutInputVertical() {

        if (m_ItemSize == Vector2.zero && itemPrefab != null)
        {
            m_ItemSize = itemPrefab.GetComponent<RectTransform>().sizeDelta;
        }
        m_ContentHeight = (m_ItemSize.y * m_TotalCount) + (m_Spacing * (m_TotalCount > 0 ? (m_TotalCount - 1) : 0)) + padding.vertical;

        SetLayoutInputForAxis(m_ContentHeight, m_ContentHeight, 0, 1);
    }
    public override void SetLayoutHorizontal() { }

    public override void SetLayoutVertical()
    {
        // --- 步骤一：设置Content的最终高度 ---
        float viewHeight = scrollRect.viewport.rect.height;
        // Content的高度，取“内容所需高度”和“视口高度”中的较大值
        float finalContentHeight = Mathf.Max(m_ContentHeight, viewHeight);

        // 只有当计算出的新高度与当前高度有显著差异时，才进行设置，以避免无限重建
        if (!Mathf.Approximately(rectTransform.sizeDelta.y, finalContentHeight))
        {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, finalContentHeight);
        }
        UpdateVisibleItems();
    }

    private void UpdateVisibleItems()
    {
        if (m_ItemSize.y <= 0 || m_TotalCount == 0)
        {
            RecycleAllActiveItems();
            return;
        }

        //计算当前帧需要显示的索引范围
        float startYOffset = GetStartOffset(1, m_ContentHeight - padding.vertical);

        float viewHeight = scrollRect.viewport.rect.height;
        float contentPos = rectTransform.anchoredPosition.y;
        float viewTopPosInContent = contentPos - startYOffset;

        // 可见区域的起始Item索引
        int newStartIndex = Mathf.FloorToInt(viewTopPosInContent / (m_ItemSize.y + m_Spacing));
        newStartIndex = Mathf.Max(0, newStartIndex);

        float viewBottomPosInContent = viewTopPosInContent + viewHeight;

        // 可见区域的结束Item索引
        int newEndIndex = Mathf.CeilToInt(viewBottomPosInContent / (m_ItemSize.y + m_Spacing));
        newEndIndex = Mathf.Min(m_TotalCount - 1, newEndIndex);

        if (newStartIndex == m_LastStartIndex && newEndIndex == m_LastEndIndex)
        {
            return;
        }

        //增量式回收 (只回收完全离开视野的) ---
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

            PositionItem(item, i, startYOffset);

            UpdateItem(item.gameObject, i);
        }

        //更新上一帧的索引记录，并启动回收
        m_LastStartIndex = newStartIndex;
        m_LastEndIndex = newEndIndex;
        RecycleItems();
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

    private void PositionItem(RectTransform item, int dataIndex, float startYPos)
    {
        item.anchorMin = new Vector2(0, 1);
        item.anchorMax = new Vector2(0, 1);

        // --- 垂直位置 (Y) ---
        // Y坐标 = - (上内边距 + 索引 * (Item高度 + 间距))
        // 这个计算是相对于顶部锚点的，所以是负值
        float yPos = -startYPos - (padding.top + dataIndex * (m_ItemSize.y + m_Spacing));

        // --- 水平位置 (X)，支持对齐 ---
        float xPos = 0;

        // 获取父容器（Content）的可用宽度
        float contentWidth = rectTransform.rect.width - padding.horizontal;
        // 获取Item自身的宽度
        float itemWidth = item.sizeDelta.x;

        // 计算水平对齐的偏移量
        // GetAlignmentOnAxis(0) -> 0 for Left, 0.5 for Center, 1 for Right
        float horizontalAlignment = GetAlignmentOnAxis(0);
        xPos = padding.left + (contentWidth - itemWidth) * horizontalAlignment;

        // 最后，将计算出的位置应用到anchoredPosition
        item.anchoredPosition = new Vector2(xPos, yPos);
    }

    private void RecycleAllActiveItems()
    {
        if (m_ActiveItems.Count == 0) return;

        foreach (var pair in m_ActiveItems)
        {
            m_ItemsToRecycle.Add(pair.Value);
        }
        m_ActiveItems.Clear();
        RecycleItems();
    }

    protected override int GetFirstVisibleItemIndex()
    {
        if (m_ItemSize.y <= 0) return 0;

        float startYOffset = GetStartOffset(1, m_ContentHeight - padding.vertical);
        float contentPos = rectTransform.anchoredPosition.y;
        float viewTopPosInContent = contentPos - startYOffset;

        int index = Mathf.FloorToInt(viewTopPosInContent / (m_ItemSize.y + m_Spacing));
        return Mathf.Max(0, index);
    }

    protected override Vector2 GetPositionForIndex(int index)
    {
        // Y 坐标 = 顶部内边距 + 锚点索引之前的总高度
        float yPos = padding.top + index * (m_ItemSize.y + m_Spacing);

        return new Vector2(rectTransform.anchoredPosition.x, yPos);
    }

    protected override float NormalizedPosition
    {
        get { return scrollRect.verticalNormalizedPosition; }
        set { scrollRect.verticalNormalizedPosition = value; }
    }

    public override void ScrollTo(int index, bool immediate = false)
    {
        if (m_TotalCount <= 0) return;
        index = Mathf.Clamp(index, 0, m_TotalCount - 1);

        // 计算目标位置的归一化坐标 (0-1)
        float itemPosY = (padding.top + index * (m_ItemSize.y + m_Spacing));
        float totalHeight = rectTransform.sizeDelta.y;
        float viewHeight = scrollRect.viewport.rect.height;

        float scrollableHeight = totalHeight - viewHeight;
        if (scrollableHeight <= 0)
        {
            DoScrollTo(1f, immediate);
            return;
        }
        // 1.0f 对应顶部, 0.0f 对应底部
        float targetNormalizedPos = 1.0f - (itemPosY / scrollableHeight);
        targetNormalizedPos = Mathf.Clamp01(targetNormalizedPos);

        DoScrollTo(targetNormalizedPos, immediate);
    }
}