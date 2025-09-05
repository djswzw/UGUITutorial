using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;

[RequireComponent(typeof(Image))]
public class RadialSlider : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("视觉表现 (Visuals)")]
    [Tooltip("填充区域从0%到100%的颜色渐变")]
    [SerializeField] private Color m_StartColor = Color.green;
    [SerializeField] private Color m_EndColor = Color.red;

    /// <summary>
    /// 当滑块的值发生变化时触发的事件。
    /// 参数为0到1之间的归一化值。
    /// </summary>
    [Serializable]
    public class RadialSliderValueChangedEvent : UnityEvent<float> { }

    [Header("事件回调 (Events)")]
    [SerializeField]
    private RadialSliderValueChangedEvent m_OnValueChanged = new RadialSliderValueChangedEvent();
    public RadialSliderValueChangedEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }


    /// <summary>
    /// 获取或设置滑块的当前值（范围 0.0f 到 1.0f）。
    /// </summary>
    public float Value
    {
        get
        {
            // 确保Image组件有效
            if (m_Image == null) return 0f;
            return m_Image.fillAmount;
        }
        set
        {
            SetValue(value);
        }
    }

    /// <summary>
    /// 获取或设置滑块的当前角度（范围 0 到 360）。
    /// </summary>
    public float Angle
    {
        get { return Value * 360f; }
        set { Value = value / 360f; }
    }

    private Image m_Image;
    private RectTransform m_RectTransform;
    private bool m_IsPointerDown = false;
    private Camera m_EventCamera;

    private void Awake()
    {
        m_Image = GetComponent<Image>();
        m_RectTransform = transform as RectTransform;

        // 在初始化时，强制将Image组件设置为我们需要的模式，以提供更好的用户体验
        if (m_Image.type != Image.Type.Filled || m_Image.fillMethod != Image.FillMethod.Radial360)
        {
            m_Image.type = Image.Type.Filled;
            m_Image.fillMethod = Image.FillMethod.Radial360;
            Debug.LogWarning("RadialSlider: Image component's type and fillMethod have been automatically set to Filled and Radial360.", this);
        }

        // 通常，径向滑块的填充起点应该是固定的，例如顶部
        m_Image.fillOrigin = (int)Image.Origin360.Top;
        m_Image.fillClockwise = true; // 通常是顺时针填充
    }


    /// <summary>
    /// 统一的值设置入口，负责约束值、更新视觉和触发回调。
    /// </summary>
    /// <param name="value">要设置的0-1之间的值</param>
    /// <param name="sendCallback">是否触发onValueChanged事件</param>
    private void SetValue(float value, bool sendCallback = true)
    {
        value = Mathf.Clamp01(value);

        // 如果值没有发生足够大的变化，则不进行任何操作，以优化性能
        if (Mathf.Approximately(Value, value))
            return;

        UpdateVisuals(value);

        if (sendCallback)
        {
            m_OnValueChanged.Invoke(Value);
        }
    }

    /// <summary>
    /// 根据给定的0-1值，更新Image的fillAmount和颜色。
    /// </summary>
    private void UpdateVisuals(float value)
    {
        if (m_Image == null) return;

        m_Image.fillAmount = value;
        m_Image.color = Color.Lerp(m_StartColor, m_EndColor, value);
    }

    /// <summary>
    /// 核心算法：将屏幕坐标转换为0-1的归一化角度值。
    /// </summary>
    private float GetValueFromPointerPosition(Vector2 screenPosition)
    {
        Vector2 localPointerPos;
        // 使用RectTransformUtility将屏幕坐标转换为RectTransform的本地坐标
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_RectTransform, screenPosition, m_EventCamera, out localPointerPos))
        {
            // 如果转换失败（例如指针在矩形外），则返回当前值以避免跳变
            return Value;
        }

        // Atan2函数需要以(0,0)为中心点来计算角度，但localPointerPos的原点是pivot。
        // 我们需要先进行一次坐标系平移，将坐标转换为以矩形几何中心为原点。
        Vector2 pivotOffset = m_RectTransform.rect.size * (m_RectTransform.pivot - new Vector2(0.5f, 0.5f));
        Vector2 centerRelativePos = localPointerPos + pivotOffset;

        // 使用Atan2计算角度。它返回的是弧度，范围是 -PI 到 PI。
        // Y轴在前，X轴在后，这是标准的数学库用法。
        float angleRad = Mathf.Atan2(centerRelativePos.y, centerRelativePos.x);

        // 将弧度转换为度，范围 -180 到 180。
        float angleDeg = angleRad * Mathf.Rad2Deg;

        // 将角度范围调整到 0 到 360。
        if (angleDeg < 0)
        {
            angleDeg += 360f;
        }

        // UGUI Image.FillMethod.Radial360的0度位置，取决于fillOrigin的设置。
        // 为了与我们Awake中设置的fillOrigin = Top (90度) 对应，我们需要进行一次旋转校正。
        // Atan2的0度在右侧，我们需要将90度（顶部）作为新的0点。
        angleDeg = (angleDeg - 90f + 360f) % 360f;

        // 如果是顺时针填充，我们需要反转角度值。
        if (m_Image.fillClockwise)
        {
            angleDeg = 360f - angleDeg;
        }

        // 最终归一化到0-1
        return angleDeg / 360f;
    }


    /// <summary>
    /// 当指针进入UI元素区域时调用。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 缓存用于坐标转换的相机。使用pressEventCamera比enterEventCamera更可靠。
        m_EventCamera = eventData.pressEventCamera;
    }

    /// <summary>
    /// 当指针在UI元素上按下时调用。
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        m_IsPointerDown = true;
        m_EventCamera = eventData.pressEventCamera;

        // 在按下时，也应该立即响应一次，让滑块能“指哪打哪”。
        OnDrag(eventData);
    }

    /// <summary>
    /// 当指针抬起时调用。
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        m_IsPointerDown = false;
    }

    /// <summary>
    /// 当指针在UI元素上拖动时，每一帧都会调用。
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (m_IsPointerDown)
        {
            float newValue = GetValueFromPointerPosition(eventData.position);
            SetValue(newValue);
        }
    }
}