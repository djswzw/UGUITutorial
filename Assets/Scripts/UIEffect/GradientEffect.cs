using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Pool;

[AddComponentMenu("UI/Effects/Extensions/Gradient Effect")]
public class GradientEffect : BaseMeshEffect
{
    /// <summary>
    /// 渐变的方向。
    /// </summary>
    public enum GradientDirection
    {
        Vertical,       // 垂直
        Horizontal,     // 水平
        FourCorners     // 四角
    }

    [Tooltip("渐变的应用方向。")]
    public GradientDirection direction = GradientDirection.Vertical;

    [Header("垂直渐变")]
    [Tooltip("UI元素顶部的颜色。")]
    public Color colorTop = new Color(1f, 1f, 1f, 1f);
    [Tooltip("UI元素底部的颜色。")]
    public Color colorBottom = new Color(0f, 0f, 0f, 1f);

    [Header("水平渐变")]
    [Tooltip("UI元素左侧的颜色。")]
    public Color colorLeft = new Color(1f, 1f, 1f, 1f);
    [Tooltip("UI元素右侧的颜色。")]
    public Color colorRight = new Color(0f, 0f, 0f, 1f);

    // 备注: 对于四角渐变模式，以上四个颜色属性都会被使用。
    // colorBottom 代表左下角，colorLeft 代表右下角
    // colorTop 代表左上角，colorRight 代表右上角

    public override void ModifyMesh(VertexHelper vh)
    {
        // 如果组件未激活，或没有顶点，则不执行任何操作。
        if (!IsActive() || vh.currentVertCount == 0)
        {
            return;
        }

        // 使用ListPool从对象池中获取一个临时的顶点列表，以避免GC开销。
        var vertexList = ListPool<UIVertex>.Get();
        vh.GetUIVertexStream(vertexList);

        try
        {
            // 核心的顶点修改逻辑被封装在此方法中。
            ModifyVertices(vertexList);

            // 清空VertexHelper，并将修改后的顶点流重新写入。
            vh.Clear();
            vh.AddUIVertexTriangleStream(vertexList);
        }
        finally
        {
            // 使用try-finally结构，确保即使发生异常，列表也一定会被释放回对象池。
            ListPool<UIVertex>.Release(vertexList);
        }
    }

    private void ModifyVertices(List<UIVertex> vertexList)
    {
        int count = vertexList.Count;
        if (count == 0) return;

        // 步骤 1: 精确计算UI元素的几何边界 (轴对齐包围盒, AABB)。
        // 这一步对于让渐变效果在复杂Sprite上（如Sliced, Tiled或有紧密网格的Sprite）正确工作至关重要。
        float bottomY = vertexList[0].position.y;
        float topY = vertexList[0].position.y;
        float leftX = vertexList[0].position.x;
        float rightX = vertexList[0].position.x;

        for (int i = 1; i < count; i++)
        {
            float y = vertexList[i].position.y;
            if (y > topY)
            {
                topY = y;
            }
            else if (y < bottomY)
            {
                bottomY = y;
            }

            float x = vertexList[i].position.x;
            if (x > rightX)
            {
                rightX = x;
            }
            else if (x < leftX)
            {
                leftX = x;
            }
        }

        float uiElementHeight = topY - bottomY;
        float uiElementWidth = rightX - leftX;

        // 步骤 2: 遍历每一个顶点，并应用计算出的渐变颜色。
        UIVertex vertex = new UIVertex();
        for (int i = 0; i < count; i++)
        {
            vertex = vertexList[i];

            Color colorMultiplier;

            switch (direction)
            {
                case GradientDirection.Vertical:
                    // 计算归一化的垂直位置（0在底部，1在顶部）。
                    float normalizedY = (uiElementHeight > 0) ? (vertex.position.y - bottomY) / uiElementHeight : 0f;
                    colorMultiplier = Color.Lerp(colorBottom, colorTop, normalizedY);
                    break;

                case GradientDirection.Horizontal:
                    // 计算归一化的水平位置（0在左侧，1在右侧）。
                    float normalizedX = (uiElementWidth > 0) ? (vertex.position.x - leftX) / uiElementWidth : 0f;
                    colorMultiplier = Color.Lerp(colorLeft, colorRight, normalizedX);
                    break;

                case GradientDirection.FourCorners:
                    // 计算水平和垂直的归一化位置。
                    float hLerp = (uiElementWidth > 0) ? (vertex.position.x - leftX) / uiElementWidth : 0f;
                    float vLerp = (uiElementHeight > 0) ? (vertex.position.y - bottomY) / uiElementHeight : 0f;

                    // 进行双线性插值。
                    // 首先，沿着底部和顶部边缘，根据水平位置进行线性插值。
                    Color bottomLerp = Color.Lerp(colorBottom, colorLeft, hLerp); // 假设 colorBottom是左下, colorLeft是右下
                    Color topLerp = Color.Lerp(colorTop, colorRight, hLerp);      // 假设 colorTop是左上, colorRight是右上

                    // 然后，根据垂直位置，在上下两条边的插值结果之间，再次进行线性插值。
                    colorMultiplier = Color.Lerp(bottomLerp, topLerp, vLerp);
                    break;

                default:
                    colorMultiplier = Color.white;
                    break;
            }

            // 将计算出的渐变色 与 顶点的原始颜色 进行正片叠底（相乘）。
            // 这样做可以保留原有的颜色信息（例如来自富文本标签的颜色）以及Graphic组件的主颜色。
            // UIVertex.color是Color32类型，所以我们先转成Color进行运算，再转回去。
            var originalColor = (Color)vertex.color;
            vertex.color = (Color32)(originalColor * colorMultiplier);

            vertexList[i] = vertex;
        }
    }
}