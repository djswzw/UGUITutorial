// 特性: 支持单击、双击、多次点击和长按事件。
// 使用方法:
// 1. 将此脚本附加到你的Button GameObject上，它会自动取代原有的Button功能。
// 2. 在Inspector面板中，为onSingleClick, onDoubleClick, onMultiClick, onLongPressStart等事件配置回调。
// 3. 根据需要调整multiClickThreshold和longPressThreshold参数。

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 一个支持单击、双击、多次点击和长按事件的按钮。
/// </summary>
[AddComponentMenu("UI/Extensions/Advanced Button")]
public class AdvancedButton : Button
{
    #region Public Events & Properties

    [System.Serializable]
    public class ButtonMultiClickedEvent : UnityEvent<int> { }

    [Header("事件 (Advanced Events)")]
    [Tooltip("当发生单击（Single Click）时调用。在双击的第一次点击时不会触发。")]
    public ButtonClickedEvent onSingleClick = new ButtonClickedEvent();

    [Tooltip("当发生双击（Double Click）时调用。")]
    public ButtonClickedEvent onDoubleClick = new ButtonClickedEvent();

    [Tooltip("当发生多次点击（Multi Click）时调用，会传递当前的连续点击次数。")]
    public ButtonMultiClickedEvent onMultiClick = new ButtonMultiClickedEvent();

    [Tooltip("当长按开始时调用（只在达到阈值时调用一次）。")]
    public ButtonClickedEvent onLongPressStart = new ButtonClickedEvent();

    [Tooltip("当长按持续时，在每一帧调用。")]
    public ButtonClickedEvent onLongPressing = new ButtonClickedEvent();

    [Tooltip("当长按结束时调用（指针抬起或移出时）。")]
    public ButtonClickedEvent onLongPressEnd = new ButtonClickedEvent();

    [Header("参数设置 (Settings)")]
    [Tooltip("判断为双击或多次点击的时间间隔（秒）。")]
    public float multiClickThreshold = 0.25f;

    [Tooltip("判断为长按所需的最短持续时间（秒）。")]
    public float longPressThreshold = 1.0f;

        public int ClickCount { get => m_ClickCount;}
    #endregion

    #region Private Fields

    // 用于多次点击检测
    private int m_ClickCount = 0;
    private float m_LastClickTime = 0f;

    // 用于长按检测
    private bool m_IsPointerDown = false;
    private bool m_IsLongPressing = false;
    private float m_PointerDownTime = 0f;



    #endregion

    #region Update Loop for Long Press

    private void Update()
    {
        // 如果指针没有按下，则无需进行长按检测
        if (!m_IsPointerDown)
        {
            return;
        }

        // 如果已经处于长按状态，则持续触发onLongPressing事件
        if (m_IsLongPressing)
        {
            onLongPressing.Invoke();
        }
        // 如果尚未进入长按状态，则检查持续时间是否已达到阈值
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
        // 阻止基类的onClick事件立即触发
        // base.OnPointerClick(eventData); 

        if (Time.unscaledTime - m_LastClickTime < multiClickThreshold)
        {
            // 如果两次点击的时间间隔小于阈值，则认为是连续点击
            m_ClickCount++;
        }
        else
        {
            // 如果超时，则重置点击次数
            m_ClickCount = 1;
        }

        m_LastClickTime = Time.unscaledTime;

        if (m_ClickCount == 2)
        {
            // 触发双击事件
            onDoubleClick.Invoke();
        }

        // 触发多次点击事件
        onMultiClick.Invoke(m_ClickCount);

        // 我们需要一个机制来区分“单击”和“双击”的第一次点击
        // 这里采用延迟调用的方式
        CancelInvoke("InvokeSingleClick"); // 取消上一次的单击调用
        if (m_ClickCount == 1)
        {
            Invoke("InvokeSingleClick", multiClickThreshold);
        }
    }

    /// <summary>
    /// 覆写OnPointerDown来开始长按的计时。
    /// </summary>
    public override void OnPointerDown(PointerEventData eventData)
    {
        // 必须调用基类方法，以确保Selectable的状态机能正确处理Pressed状态的视觉过渡
        base.OnPointerDown(eventData);

        m_IsPointerDown = true;
        m_PointerDownTime = Time.unscaledTime;
    }

    /// <summary>
    /// 覆写OnPointerUp来结束长按的计时。
    /// </summary>
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        ResetLongPressState();
    }

    /// <summary>
    /// 覆写OnPointerExit，当指针移出时也应结束长按。
    /// </summary>
    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        ResetLongPressState();
    }

    /// <summary>
    /// 当组件被禁用时，确保所有状态都被重置。
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
        // 检查在延迟时间结束后，点击次数是否仍然为1
        if (m_ClickCount == 1)
        {
            onSingleClick.Invoke();

            // 为了兼容性，手动触发基类的onClick事件
            if (IsActive() && IsInteractable())
            {
                onClick.Invoke();
            }
        }

        // 重置点击计数
        m_ClickCount = 0;
    }

    private void ResetLongPressState()
    {
        m_IsPointerDown = false;

        // 如果在状态重置前，正处于长按状态，则触发长按结束事件
        if (m_IsLongPressing)
        {
            m_IsLongPressing = false;
            onLongPressEnd.Invoke();
        }
    }

    #endregion
}