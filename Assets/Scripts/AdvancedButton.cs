// ����: ֧�ֵ�����˫������ε���ͳ����¼���
// ʹ�÷���:
// 1. ���˽ű����ӵ����Button GameObject�ϣ������Զ�ȡ��ԭ�е�Button���ܡ�
// 2. ��Inspector����У�ΪonSingleClick, onDoubleClick, onMultiClick, onLongPressStart���¼����ûص���
// 3. ������Ҫ����multiClickThreshold��longPressThreshold������

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// һ��֧�ֵ�����˫������ε���ͳ����¼��İ�ť��
/// </summary>
[AddComponentMenu("UI/Extensions/Advanced Button")]
public class AdvancedButton : Button
{
    #region Public Events & Properties

    [System.Serializable]
    public class ButtonMultiClickedEvent : UnityEvent<int> { }

    [Header("�¼� (Advanced Events)")]
    [Tooltip("������������Single Click��ʱ���á���˫���ĵ�һ�ε��ʱ���ᴥ����")]
    public ButtonClickedEvent onSingleClick = new ButtonClickedEvent();

    [Tooltip("������˫����Double Click��ʱ���á�")]
    public ButtonClickedEvent onDoubleClick = new ButtonClickedEvent();

    [Tooltip("��������ε����Multi Click��ʱ���ã��ᴫ�ݵ�ǰ���������������")]
    public ButtonMultiClickedEvent onMultiClick = new ButtonMultiClickedEvent();

    [Tooltip("��������ʼʱ���ã�ֻ�ڴﵽ��ֵʱ����һ�Σ���")]
    public ButtonClickedEvent onLongPressStart = new ButtonClickedEvent();

    [Tooltip("����������ʱ����ÿһ֡���á�")]
    public ButtonClickedEvent onLongPressing = new ButtonClickedEvent();

    [Tooltip("����������ʱ���ã�ָ��̧����Ƴ�ʱ����")]
    public ButtonClickedEvent onLongPressEnd = new ButtonClickedEvent();

    [Header("�������� (Settings)")]
    [Tooltip("�ж�Ϊ˫�����ε����ʱ�������룩��")]
    public float multiClickThreshold = 0.25f;

    [Tooltip("�ж�Ϊ�����������̳���ʱ�䣨�룩��")]
    public float longPressThreshold = 1.0f;

        public int ClickCount { get => m_ClickCount;}
    #endregion

    #region Private Fields

    // ���ڶ�ε�����
    private int m_ClickCount = 0;
    private float m_LastClickTime = 0f;

    // ���ڳ������
    private bool m_IsPointerDown = false;
    private bool m_IsLongPressing = false;
    private float m_PointerDownTime = 0f;



    #endregion

    #region Update Loop for Long Press

    private void Update()
    {
        // ���ָ��û�а��£���������г������
        if (!m_IsPointerDown)
        {
            return;
        }

        // ����Ѿ����ڳ���״̬�����������onLongPressing�¼�
        if (m_IsLongPressing)
        {
            onLongPressing.Invoke();
        }
        // �����δ���볤��״̬���������ʱ���Ƿ��Ѵﵽ��ֵ
        else if (Time.unscaledTime - m_PointerDownTime >= longPressThreshold)
        {
            m_IsLongPressing = true;
            onLongPressStart.Invoke();
        }
    }

    #endregion

    #region Event Handlers Override


    public override void OnPointerClick(PointerEventData eventData)
    {
        // ��ֹ�����onClick�¼���������
        // base.OnPointerClick(eventData); 

        if (Time.unscaledTime - m_LastClickTime < multiClickThreshold)
        {
            // ������ε����ʱ����С����ֵ������Ϊ���������
            m_ClickCount++;
        }
        else
        {
            // �����ʱ�������õ������
            m_ClickCount = 1;
        }

        m_LastClickTime = Time.unscaledTime;

        if (m_ClickCount == 2)
        {
            // ����˫���¼�
            onDoubleClick.Invoke();
        }

        // ������ε���¼�
        onMultiClick.Invoke(m_ClickCount);

        // ������Ҫһ�����������֡��������͡�˫�����ĵ�һ�ε��
        // ��������ӳٵ��õķ�ʽ
        CancelInvoke("InvokeSingleClick"); // ȡ����һ�εĵ�������
        if (m_ClickCount == 1)
        {
            Invoke("InvokeSingleClick", multiClickThreshold);
        }
    }

    /// <summary>
    /// ��дOnPointerDown����ʼ�����ļ�ʱ��
    /// </summary>
    public override void OnPointerDown(PointerEventData eventData)
    {
        // ������û��෽������ȷ��Selectable��״̬������ȷ����Pressed״̬���Ӿ�����
        base.OnPointerDown(eventData);

        m_IsPointerDown = true;
        m_PointerDownTime = Time.unscaledTime;
    }

    /// <summary>
    /// ��дOnPointerUp�����������ļ�ʱ��
    /// </summary>
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        ResetLongPressState();
    }

    /// <summary>
    /// ��дOnPointerExit����ָ���Ƴ�ʱҲӦ����������
    /// </summary>
    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        ResetLongPressState();
    }

    /// <summary>
    /// �����������ʱ��ȷ������״̬�������á�
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        CancelInvoke(nameof(InvokeSingleClick));
        ResetLongPressState();
        m_ClickCount = 0;
    }

    #endregion

    #region Private Helper Methods

    private void InvokeSingleClick()
    {
        // ������ӳ�ʱ������󣬵�������Ƿ���ȻΪ1
        if (m_ClickCount == 1)
        {
            onSingleClick.Invoke();

            // Ϊ�˼����ԣ��ֶ����������onClick�¼�
            if (IsActive() && IsInteractable())
            {
                onClick.Invoke();
            }
        }

        // ���õ������
        m_ClickCount = 0;
    }

    private void ResetLongPressState()
    {
        m_IsPointerDown = false;

        // �����״̬����ǰ�������ڳ���״̬���򴥷����������¼�
        if (m_IsLongPressing)
        {
            m_IsLongPressing = false;
            onLongPressEnd.Invoke();
        }
    }

    #endregion
}