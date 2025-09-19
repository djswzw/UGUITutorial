using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public interface ILoopScrollRect
{
    void ProvideData(int totalCount);
}

public abstract class LoopScrollRect : LayoutGroup, ILoopScrollRect
{
    #region Inspector 字段
    [Tooltip("用于实例化列表项的Prefab。Prefab上必须挂载RectTransform。")]
    public GameObject itemPrefab;
    [Tooltip("列表项之间的间距。")]
    [SerializeField] protected float m_Spacing = 0;
    public float spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }
    #endregion

    #region 核心数据
    protected int m_TotalCount = 0;
    protected Vector2 m_ItemSize = Vector2.zero;
    protected readonly Queue<RectTransform> m_ItemPool = new Queue<RectTransform>();
    protected readonly Dictionary<int, RectTransform> m_ActiveItems = new Dictionary<int, RectTransform>();

    // 用于暂存本轮更新中需要被回收的Item
    protected readonly List<RectTransform> m_ItemsToRecycle = new List<RectTransform>();

    // 协程相关的标记，确保同一时间只有一个回收协程在运行
    private Coroutine m_RecycleCoroutine = null;
    private bool m_IsRecycling = false;
    private Coroutine m_RestorePositionCoroutine = null;

    private ScrollRect m_ScrollRect;
    protected ScrollRect scrollRect
    {
        get { if (m_ScrollRect == null) m_ScrollRect = GetComponentInParent<ScrollRect>(); return m_ScrollRect; }
    }

    protected abstract float NormalizedPosition { get; set; }
    #endregion

    #region 跳转逻辑
    private Coroutine m_ScrollToCoroutine;
    [Tooltip("缓动跳转时的动画曲线")]
    public AnimationCurve scrollToCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("缓动跳转的时长（秒）")]
    public float scrollToDuration = 0.5f;
    #endregion

    #region 布局计算
    public override void CalculateLayoutInputHorizontal(){  }
    public override void CalculateLayoutInputVertical() {  }
    public override void SetLayoutHorizontal() {  }
    public override void SetLayoutVertical() {  }
    #endregion

    #region ILoopScrollRect 接口实现
    /// <summary>
    /// 1. 如果新的 totalCount 与旧的不同，则完全重建列表。
    /// 2. 如果 totalCount 保持不变，则只刷新当前可见项的内容。
    /// </summary>
    /// <param name="totalCount">列表项的总数。</param>
    public void ProvideData(int totalCount)
    {
        if (itemPrefab == null || itemPrefab.GetComponent<RectTransform>() == null)
        {
            Debug.LogError("LoopScrollRect: Item Prefab is invalid.", this);
            return;
        }

        if (m_TotalCount != totalCount)
        {
            // --- 核心修改：维持滚动位置 ---
            int anchorIndex = 0;
            // 只有在列表已经有内容时，才需要记录锚点
            if (m_TotalCount > 0)
            {
                anchorIndex = GetFirstVisibleItemIndex();
            }

            m_TotalCount = totalCount;
            m_ItemSize = itemPrefab.GetComponent<RectTransform>().sizeDelta;

            OnRebuild();
            SetDirty();

            // 停止任何正在进行的恢复协程
            if (m_RestorePositionCoroutine != null)
            {
                StopCoroutine(m_RestorePositionCoroutine);
            }

            // 启动协程，在布局更新后恢复位置
            // 如果旧列表为空，则无需恢复，让其保持在顶部
            if (anchorIndex > 0)
            {
                m_RestorePositionCoroutine = StartCoroutine(RestorePositionCoroutine(anchorIndex));
            }
        }
        else
        {
            RefreshVisibleItems();
        }
    }

    protected virtual void OnRebuild()
    {
    }

    #endregion

    #region 位置计算
    /// <summary>
    /// 获取当前视口顶部/左侧第一个可见项的数据索引。
    /// </summary>
    protected abstract int GetFirstVisibleItemIndex();

    /// <summary>
    /// 计算将指定索引的Item置于视口顶部/左侧所需的Content位置。
    /// </summary>
    protected abstract Vector2 GetPositionForIndex(int index);

    /// <summary>
    /// 协程：等待UI布局重建完成后，恢复到指定锚点的位置。
    /// </summary>
    private IEnumerator RestorePositionCoroutine(int index)
    {
        // 等待当前帧的渲染周期结束，此时UGUI的布局计算已经完成
        yield return new WaitForEndOfFrame();

        // 确保锚点索引在新数据范围内
        index = Mathf.Min(index, m_TotalCount - 1);

        // 应用计算出的新位置
        rectTransform.anchoredPosition = GetPositionForIndex(index);

        // 手动触发一次更新，防止恢复位置后Item不刷新
        SetDirty();

        m_RestorePositionCoroutine = null;
    }
    #endregion

    #region Unity 生命周期
    protected override void OnEnable()
    {
        base.OnEnable();
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScroll);
        }
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScroll);
        }

        // 确保协程在禁用时停止
        if (m_RecycleCoroutine != null)
        {
            StopCoroutine(m_RecycleCoroutine);
            m_RecycleCoroutine = null;
            m_IsRecycling = false;
        }
        if (m_ScrollToCoroutine != null) StopCoroutine(m_ScrollToCoroutine);
    }
    #endregion

    #region 事件处理
    private void OnScroll(Vector2 position)
    {
        SetDirty();
    }
    #endregion

    #region Item 更新回调
    public event System.Action<GameObject, int> OnItemUpdate;
    protected void UpdateItem(GameObject item, int dataIndex)
    {
        OnItemUpdate?.Invoke(item, dataIndex);
    }

    public void RefreshVisibleItems()
    {
        if (m_ActiveItems == null || m_ActiveItems.Count == 0) return;

        foreach (var pair in m_ActiveItems)
        {
            UpdateItem(pair.Value.gameObject, pair.Key);
        }
    }

    #endregion

    #region 协程回收
    /// <summary>
    /// 将回收操作放入一个延迟执行的协程中。
    /// </summary>
    protected void RecycleItems()
    {
        // 如果当前没有回收协程在运行，则启动一个新的
        if (!m_IsRecycling)
        {
            m_RecycleCoroutine = StartCoroutine(RecycleCoroutine());
        }
    }

    private IEnumerator RecycleCoroutine()
    {
        m_IsRecycling = true;

        // 延迟到这一帧的末尾执行，此时重建循环已经完全结束
        yield return new WaitForEndOfFrame();

        // 执行真正的回收操作
        foreach (var item in m_ItemsToRecycle)
        {
            item.gameObject.SetActive(false);
            m_ItemPool.Enqueue(item);
        }
        m_ItemsToRecycle.Clear();

        m_IsRecycling = false;
        m_RecycleCoroutine = null;
    }
    #endregion

    #region 跳转功能
    public abstract void ScrollTo(int index, bool immediate = false);

    protected void DoScrollTo(float targetNormalizedPos, bool immediate)
    {
        if (m_ScrollToCoroutine != null)
        {
            StopCoroutine(m_ScrollToCoroutine);
        }

        if (immediate)
        {
            NormalizedPosition = targetNormalizedPos;
        }
        else
        {
            m_ScrollToCoroutine = StartCoroutine(ScrollToCoroutine(targetNormalizedPos));
        }
    }

    private IEnumerator ScrollToCoroutine(float target)
    {
        float start = NormalizedPosition;
        float timer = 0;

        while (timer < scrollToDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / scrollToDuration);
            NormalizedPosition = Mathf.Lerp(start, target, scrollToCurve.Evaluate(progress));
            yield return null;
        }
        NormalizedPosition = target;
        m_ScrollToCoroutine = null;
    }
    #endregion
}