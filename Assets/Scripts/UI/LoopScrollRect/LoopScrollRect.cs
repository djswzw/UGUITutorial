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
    #region Inspector �ֶ�
    [Tooltip("����ʵ�����б����Prefab��Prefab�ϱ������RectTransform��")]
    public GameObject itemPrefab;
    [Tooltip("�б���֮��ļ�ࡣ")]
    [SerializeField] protected float m_Spacing = 0;
    public float spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }
    #endregion

    #region ��������
    protected int m_TotalCount = 0;
    protected Vector2 m_ItemSize = Vector2.zero;
    protected readonly Queue<RectTransform> m_ItemPool = new Queue<RectTransform>();
    protected readonly Dictionary<int, RectTransform> m_ActiveItems = new Dictionary<int, RectTransform>();

    // �����ݴ汾�ָ�������Ҫ�����յ�Item
    protected readonly List<RectTransform> m_ItemsToRecycle = new List<RectTransform>();

    // Э����صı�ǣ�ȷ��ͬһʱ��ֻ��һ������Э��������
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

    #region ��ת�߼�
    private Coroutine m_ScrollToCoroutine;
    [Tooltip("������תʱ�Ķ�������")]
    public AnimationCurve scrollToCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("������ת��ʱ�����룩")]
    public float scrollToDuration = 0.5f;
    #endregion

    #region ���ּ���
    public override void CalculateLayoutInputHorizontal(){  }
    public override void CalculateLayoutInputVertical() {  }
    public override void SetLayoutHorizontal() {  }
    public override void SetLayoutVertical() {  }
    #endregion

    #region ILoopScrollRect �ӿ�ʵ��
    /// <summary>
    /// 1. ����µ� totalCount ��ɵĲ�ͬ������ȫ�ؽ��б�
    /// 2. ��� totalCount ���ֲ��䣬��ֻˢ�µ�ǰ�ɼ�������ݡ�
    /// </summary>
    /// <param name="totalCount">�б����������</param>
    public void ProvideData(int totalCount)
    {
        if (itemPrefab == null || itemPrefab.GetComponent<RectTransform>() == null)
        {
            Debug.LogError("LoopScrollRect: Item Prefab is invalid.", this);
            return;
        }

        if (m_TotalCount != totalCount)
        {
            // --- �����޸ģ�ά�ֹ���λ�� ---
            int anchorIndex = 0;
            // ֻ�����б��Ѿ�������ʱ������Ҫ��¼ê��
            if (m_TotalCount > 0)
            {
                anchorIndex = GetFirstVisibleItemIndex();
            }

            m_TotalCount = totalCount;
            m_ItemSize = itemPrefab.GetComponent<RectTransform>().sizeDelta;

            OnRebuild();
            SetDirty();

            // ֹͣ�κ����ڽ��еĻָ�Э��
            if (m_RestorePositionCoroutine != null)
            {
                StopCoroutine(m_RestorePositionCoroutine);
            }

            // ����Э�̣��ڲ��ָ��º�ָ�λ��
            // ������б�Ϊ�գ�������ָ������䱣���ڶ���
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

    #region λ�ü���
    /// <summary>
    /// ��ȡ��ǰ�ӿڶ���/����һ���ɼ��������������
    /// </summary>
    protected abstract int GetFirstVisibleItemIndex();

    /// <summary>
    /// ���㽫ָ��������Item�����ӿڶ���/��������Contentλ�á�
    /// </summary>
    protected abstract Vector2 GetPositionForIndex(int index);

    /// <summary>
    /// Э�̣��ȴ�UI�����ؽ���ɺ󣬻ָ���ָ��ê���λ�á�
    /// </summary>
    private IEnumerator RestorePositionCoroutine(int index)
    {
        // �ȴ���ǰ֡����Ⱦ���ڽ�������ʱUGUI�Ĳ��ּ����Ѿ����
        yield return new WaitForEndOfFrame();

        // ȷ��ê�������������ݷ�Χ��
        index = Mathf.Min(index, m_TotalCount - 1);

        // Ӧ�ü��������λ��
        rectTransform.anchoredPosition = GetPositionForIndex(index);

        // �ֶ�����һ�θ��£���ֹ�ָ�λ�ú�Item��ˢ��
        SetDirty();

        m_RestorePositionCoroutine = null;
    }
    #endregion

    #region Unity ��������
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

        // ȷ��Э���ڽ���ʱֹͣ
        if (m_RecycleCoroutine != null)
        {
            StopCoroutine(m_RecycleCoroutine);
            m_RecycleCoroutine = null;
            m_IsRecycling = false;
        }
        if (m_ScrollToCoroutine != null) StopCoroutine(m_ScrollToCoroutine);
    }
    #endregion

    #region �¼�����
    private void OnScroll(Vector2 position)
    {
        SetDirty();
    }
    #endregion

    #region Item ���»ص�
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

    #region Э�̻���
    /// <summary>
    /// �����ղ�������һ���ӳ�ִ�е�Э���С�
    /// </summary>
    protected void RecycleItems()
    {
        // �����ǰû�л���Э�������У�������һ���µ�
        if (!m_IsRecycling)
        {
            m_RecycleCoroutine = StartCoroutine(RecycleCoroutine());
        }
    }

    private IEnumerator RecycleCoroutine()
    {
        m_IsRecycling = true;

        // �ӳٵ���һ֡��ĩβִ�У���ʱ�ؽ�ѭ���Ѿ���ȫ����
        yield return new WaitForEndOfFrame();

        // ִ�������Ļ��ղ���
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

    #region ��ת����
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