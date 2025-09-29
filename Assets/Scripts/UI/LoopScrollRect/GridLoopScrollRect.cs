using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("UI/Extensions/Loop ScrollRect/Grid Loop ScrollRect")]
public class GridLoopScrollRect : LoopScrollRect
{
    private float m_ContentWidth = 0;
    private float m_ContentHeight = 0;

    public enum EConstraint
    {
        FixedColumnCount,
        FixedRowCount
    }

    [SerializeField]
    private EConstraint m_Constraint = EConstraint.FixedColumnCount;
    public EConstraint constraint
    {
        get { return m_Constraint; }
        set
        {
            if (m_Constraint != value)
            {
                m_Constraint = value;
                SetDirty();
            }
        }
    }

    [SerializeField]
    private int m_ConstraintCount = 4;
    public int constraintCount
    {
        get { return m_ConstraintCount; }
        set
        {
            if (m_ConstraintCount != value)
            {
                m_ConstraintCount = value;
                SetDirty();
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
    }

    #region Layout Calculation

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        m_ContentWidth = 0;
        if (m_Constraint == EConstraint.FixedRowCount && m_ConstraintCount > 0)
        {
            int parallelAxisItemCount = Mathf.CeilToInt((float)m_TotalCount / m_ConstraintCount);
            m_ContentWidth = padding.horizontal +
                             (m_ItemSize.x * parallelAxisItemCount) +
                             (m_Spacing * (parallelAxisItemCount > 0 ? parallelAxisItemCount - 1 : 0));
        }
        SetLayoutInputForAxis(m_ContentWidth, m_ContentWidth, 0, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        base.CalculateLayoutInputVertical();
        m_ContentHeight = 0;
        if (m_Constraint == EConstraint.FixedColumnCount && m_ConstraintCount > 0)
        {
            int parallelAxisItemCount = Mathf.CeilToInt((float)m_TotalCount / m_ConstraintCount);
            m_ContentHeight = padding.vertical +
                              (m_ItemSize.y * parallelAxisItemCount) +
                              (m_Spacing * (parallelAxisItemCount > 0 ? parallelAxisItemCount - 1 : 0));
        }
        SetLayoutInputForAxis(m_ContentHeight, m_ContentHeight, 0, 1);
    }

    public override void SetLayoutHorizontal()
    {
        if (m_Constraint == EConstraint.FixedRowCount)
        {
            float viewWidth = scrollRect.viewport.rect.width;
            float finalContentWidth = Mathf.Max(m_ContentWidth, viewWidth);
            rectTransform.sizeDelta = new Vector2(finalContentWidth, rectTransform.sizeDelta.y);
            UpdateVisibleItems();
        }
    }

    public override void SetLayoutVertical()
    {
        if (m_Constraint == EConstraint.FixedColumnCount)
        {
            float viewHeight = scrollRect.viewport.rect.height;
            float finalContentHeight = Mathf.Max(m_ContentHeight, viewHeight);
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, finalContentHeight);
            UpdateVisibleItems();
        }
    }


    #endregion

    #region Item Management

    private void UpdateVisibleItems()
    {
        if (m_TotalCount == 0 || m_ConstraintCount <= 0)
        {
            RecycleAllActiveItems();
            return;
        }

        int startIndex, endIndex;

        if (m_Constraint == EConstraint.FixedColumnCount)
        {
            float totalContentHeight = m_ContentHeight - padding.vertical;
            float startYOffset = GetStartOffset(1, totalContentHeight);
            float viewHeight = scrollRect.viewport.rect.height;
            float contentPos = rectTransform.anchoredPosition.y;
            float viewTopPosInContent = contentPos - startYOffset;

            int startRow = Mathf.FloorToInt(viewTopPosInContent / (m_ItemSize.y + m_Spacing));
            startRow = Mathf.Max(0, startRow);
            int endRow = Mathf.CeilToInt((viewTopPosInContent + viewHeight) / (m_ItemSize.y + m_Spacing));
            endRow = Mathf.Max(0, endRow);

            startIndex = startRow * m_ConstraintCount;
            endIndex = (endRow + 1) * m_ConstraintCount - 1;
        }
        else // FixedRowCount
        {
            float totalContentWidth = m_ContentWidth - padding.horizontal;
            float startXOffset = GetStartOffset(0, totalContentWidth);
            float viewWidth = scrollRect.viewport.rect.width;
            float contentPos = -rectTransform.anchoredPosition.x;
            float viewLeftPosInContent = contentPos - startXOffset;

            int startCol = Mathf.FloorToInt(viewLeftPosInContent / (m_ItemSize.x + m_Spacing));
            startCol = Mathf.Max(0, startCol);
            int endCol = Mathf.CeilToInt((viewLeftPosInContent + viewWidth) / (m_ItemSize.x + m_Spacing));
            endCol = Mathf.Max(0, endCol);

            startIndex = startCol * m_ConstraintCount;
            endIndex = (endCol + 1) * m_ConstraintCount - 1;
        }

        endIndex = Mathf.Min(m_TotalCount - 1, endIndex);

        var keysToRemove = new List<int>();
        foreach (var pair in m_ActiveItems)
        {
            if (pair.Key < startIndex || pair.Key > endIndex)
            {
                keysToRemove.Add(pair.Key);
                m_ItemsToRecycle.Add(pair.Value);
            }
        }
        foreach (var key in keysToRemove) m_ActiveItems.Remove(key);

        for (int i = startIndex; i <= endIndex; i++)
        {
            if (m_ActiveItems.ContainsKey(i)) continue;
            RectTransform item = GetOrCreateItem();
            m_ActiveItems.Add(i, item);
            PositionItem(item, i);
            UpdateItem(item.gameObject, i);
        }
        if (m_ItemsToRecycle.Count > 0) RecycleItems();
    }

    protected RectTransform GetOrCreateItem()
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

    protected void RecycleAllActiveItems()
    {
        if (m_ActiveItems.Count == 0) return;

        foreach (var pair in m_ActiveItems)
        {
            m_ItemsToRecycle.Add(pair.Value);
        }
        m_ActiveItems.Clear();

        if (m_ItemsToRecycle.Count > 0)
        {
            RecycleItems();
        }
    }

    private void PositionItem(RectTransform item, int dataIndex)
    {
        item.sizeDelta = m_ItemSize;

        int row, col;
        if (m_Constraint == EConstraint.FixedColumnCount)
        {
            row = dataIndex / m_ConstraintCount;
            col = dataIndex % m_ConstraintCount;

            float totalContentHeight = m_ContentHeight - padding.vertical;
            float startYOffset = GetStartOffset(1, totalContentHeight);

            float itemsInRowWidth = m_ConstraintCount * m_ItemSize.x + (m_ConstraintCount > 0 ? m_ConstraintCount - 1 : 0) * m_Spacing;
            float availableWidth = rectTransform.rect.width - padding.horizontal;
            float horizontalAlignment = GetAlignmentOnAxis(0);
            float blockStartX = (availableWidth - itemsInRowWidth) * horizontalAlignment;

            float xPos = padding.left + blockStartX + col * (m_ItemSize.x + m_Spacing);
            float yPos = -startYOffset - padding.top - row * (m_ItemSize.y + m_Spacing);
            item.anchoredPosition = new Vector2(xPos, yPos);
        }
        else // FixedRowCount
        {
            col = dataIndex / m_ConstraintCount;
            row = dataIndex % m_ConstraintCount;

            float totalContentWidth = m_ContentWidth - padding.horizontal;
            float startXOffset = GetStartOffset(0, totalContentWidth);

            float itemsInColHeight = m_ConstraintCount * m_ItemSize.y + (m_ConstraintCount > 0 ? m_ConstraintCount - 1 : 0) * m_Spacing;
            float availableHeight = rectTransform.rect.height - padding.vertical;
            float verticalAlignment = GetAlignmentOnAxis(1);
            float blockStartY = (availableHeight - itemsInColHeight) * verticalAlignment;

            float xPos = startXOffset + padding.left + col * (m_ItemSize.x + m_Spacing);
            float yPos = -padding.top - blockStartY - row * (m_ItemSize.y + m_Spacing);
            item.anchoredPosition = new Vector2(xPos, yPos);
        }
    }

    #endregion

    #region ScrollTo & Position Restoration

    public override void ScrollTo(int index, bool immediate = false)
    {
        if (m_TotalCount <= 0 || m_ConstraintCount <= 0) return;
        index = Mathf.Clamp(index, 0, m_TotalCount - 1);

        float targetNormalizedPos;
        if (m_Constraint == EConstraint.FixedColumnCount)
        {
            int targetRow = index / m_ConstraintCount;
            float itemPosY = padding.top + targetRow * (m_ItemSize.y + m_Spacing);
            float scrollableHeight = m_ContentHeight - scrollRect.viewport.rect.height;
            if (scrollableHeight <= 0) { DoScrollTo(1f, immediate); return; }
            targetNormalizedPos = 1.0f - (itemPosY / scrollableHeight);
        }
        else // FixedRowCount
        {
            int targetCol = index / m_ConstraintCount;
            float itemPosX = padding.left + targetCol * (m_ItemSize.x + m_Spacing);
            float scrollableWidth = m_ContentWidth - scrollRect.viewport.rect.width;
            if (scrollableWidth <= 0) { DoScrollTo(0f, immediate); return; }
            targetNormalizedPos = itemPosX / scrollableWidth;
        }
        DoScrollTo(Mathf.Clamp01(targetNormalizedPos), immediate);
    }

    protected override int GetFirstVisibleItemIndex()
    {
        if (m_Constraint == EConstraint.FixedColumnCount)
        {
            float startYOffset = GetStartOffset(1, m_ContentHeight - padding.vertical);
            float viewTopPosInContent = rectTransform.anchoredPosition.y - startYOffset;
            int firstVisibleRow = Mathf.FloorToInt(viewTopPosInContent / (m_ItemSize.y + m_Spacing));
            return Mathf.Max(0, firstVisibleRow) * m_ConstraintCount;
        }
        else
        {
            float startXOffset = GetStartOffset(0, m_ContentWidth - padding.horizontal);
            float viewLeftPosInContent = -rectTransform.anchoredPosition.x - startXOffset;
            int firstVisibleCol = Mathf.FloorToInt(viewLeftPosInContent / (m_ItemSize.x + m_Spacing));
            return Mathf.Max(0, firstVisibleCol) * m_ConstraintCount;
        }
    }

    protected override Vector2 GetPositionForIndex(int index)
    {
        if (m_Constraint == EConstraint.FixedColumnCount)
        {
            int targetRow = index / m_ConstraintCount;
            float yPos = padding.top + targetRow * (m_ItemSize.y + m_Spacing);
            return new Vector2(rectTransform.anchoredPosition.x, yPos);
        }
        else
        {
            int targetCol = index / m_ConstraintCount;
            float xPos = -(padding.left + targetCol * (m_ItemSize.x + m_Spacing));
            return new Vector2(xPos, rectTransform.anchoredPosition.y);
        }
    }

    protected override float NormalizedPosition
    {
        get { return m_Constraint == EConstraint.FixedColumnCount ? scrollRect.verticalNormalizedPosition : scrollRect.horizontalNormalizedPosition; }
        set { if (m_Constraint == EConstraint.FixedColumnCount) scrollRect.verticalNormalizedPosition = value; else scrollRect.horizontalNormalizedPosition = value; }
    }

    #endregion
}