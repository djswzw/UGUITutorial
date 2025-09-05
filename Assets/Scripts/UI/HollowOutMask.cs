using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一个高性能的、动态追踪单个UI目标的镂空遮罩。
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
[AddComponentMenu("UI/Extensions/Hollow Out Mask")]
public class HollowOutMask : MaskableGraphic, ICanvasRaycastFilter
{
    #region Private Fields

    private RectTransform _target;
    private readonly Vector3[] _targetWorldCorners = new Vector3[4];

    // 用于边界计算
    private Vector3 _targetMin = Vector3.zero;
    private Vector3 _targetMax = Vector3.zero;

    // 用于高效的变化检测
    private Rect _lastSelfRect;
    private Matrix4x4 _lastTargetMatrix;
    private bool _isTargetNull = true;

    #endregion

    #region Public Methods

    /// <summary>
    /// 设置镂空的目标。
    /// </summary>
    /// <param name="target">要镂空的RectTransform。如果为null，则移除镂空。</param>
    public void SetTarget(RectTransform target)
    {
        _target = target;
        _isTargetNull = _target == null;

        // 立即强制刷新一次，以应用新的目标
        ForceRefresh();
    }

    #endregion

    #region Unity Lifecycle

    protected override void OnEnable()
    {
        base.OnEnable();
        // 组件启用时，强制刷新一次以确保状态正确
        ForceRefresh();
    }

    /// <summary>
    /// 使用LateUpdate来追踪目标的变化，以确保获取到的是当帧最终的布局结果。
    /// </summary>
    void LateUpdate()
    {
        if (_isTargetNull)
        {
            // 如果目标为空，但之前有目标，则需要刷新一次以“闭合”镂空
            if (_targetMin != Vector3.zero || _targetMax != Vector3.zero)
            {
                _targetMin = Vector3.zero;
                _targetMax = Vector3.zero;
                SetVerticesDirty();
            }
            return;
        }

        // --- 高效的变化检测 ---
        bool selfRectChanged = (_lastSelfRect != rectTransform.rect);
        bool targetMatrixChanged = (_lastTargetMatrix != _target.localToWorldMatrix);

        // 只有当自身或目标Transform发生变化时，才进行后续的昂贵计算
        if (selfRectChanged || targetMatrixChanged)
        {
            ForceRefresh();
        }
    }

    #endregion

    #region Core Logic

    private void ForceRefresh()
    {
        _lastSelfRect = rectTransform.rect;

        if (_isTargetNull)
        {
            _lastTargetMatrix = Matrix4x4.identity;
            _targetMin = Vector3.zero;
            _targetMax = Vector3.zero;
        }
        else
        {
            _lastTargetMatrix = _target.localToWorldMatrix;

            // --- 坐标系变换与边界计算 ---
            _target.GetWorldCorners(_targetWorldCorners);

            Matrix4x4 selfWorldToLocal = rectTransform.worldToLocalMatrix;
            Vector3 vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int i = 0; i < 4; i++)
            {
                Vector3 localPoint = selfWorldToLocal.MultiplyPoint3x4(_targetWorldCorners[i]);
                vMin = Vector3.Min(vMin, localPoint);
                vMax = Vector3.Max(vMax, localPoint);
            }

            _targetMin = vMin;
            _targetMax = vMax;
        }

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_isTargetNull)
        {
            // 如果没有目标，则绘制一个完整的遮罩
            base.OnPopulateMesh(vh);
            return;
        }

        // Outer Rect Vertices (相对于自身pivot)
        float outerLx = rectTransform.rect.xMin;
        float outerBy = rectTransform.rect.yMin;
        float outerRx = rectTransform.rect.xMax;
        float outerTy = rectTransform.rect.yMax;

        vh.AddVert(new Vector3(outerLx, outerTy), color, Vector2.zero); // 0: Outer LT
        vh.AddVert(new Vector3(outerRx, outerTy), color, Vector2.zero); // 1: Outer RT
        vh.AddVert(new Vector3(outerRx, outerBy), color, Vector2.zero); // 2: Outer RB
        vh.AddVert(new Vector3(outerLx, outerBy), color, Vector2.zero); // 3: Outer LB

        // Inner Rect Vertices (在自身本地坐标系下)
        float innerLx = _targetMin.x;
        float innerBy = _targetMin.y;
        float innerRx = _targetMax.x;
        float innerTy = _targetMax.y;

        vh.AddVert(new Vector3(innerLx, innerTy), color, Vector2.zero); // 4: Inner LT
        vh.AddVert(new Vector3(innerRx, innerTy), color, Vector2.zero); // 5: Inner RT
        vh.AddVert(new Vector3(innerRx, innerBy), color, Vector2.zero); // 6: Inner RB
        vh.AddVert(new Vector3(innerLx, innerBy), color, Vector2.zero); // 7: Inner LB

        // Triangulation (8 triangles to form a hollow rect)
        // Top side
        vh.AddTriangle(0, 1, 5);
        vh.AddTriangle(5, 4, 0);
        // Right side
        vh.AddTriangle(1, 2, 6);
        vh.AddTriangle(6, 5, 1);
        // Bottom side
        vh.AddTriangle(2, 3, 7);
        vh.AddTriangle(7, 6, 2);
        // Left side
        vh.AddTriangle(3, 0, 4);
        vh.AddTriangle(4, 7, 3);
    }

    #endregion

    #region Raycast Filtering

    public bool IsRaycastLocationValid(Vector2 screenPos, Camera eventCamera)
    {
        // 如果组件禁用，则不参与射线检测（默认行为）
        if (!isActiveAndEnabled)
            return true;

        // 如果没有目标，HollowOutMask自身作为一个Graphic，应该阻挡事件。
        if (_isTargetNull)
            return true;

        // 核心逻辑：如果点击点在目标内部，则“无效”（让射线穿过）
        return !RectTransformUtility.RectangleContainsScreenPoint(_target, screenPos, eventCamera);
    }

    #endregion
}